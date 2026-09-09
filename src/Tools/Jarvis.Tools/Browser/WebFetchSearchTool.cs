using Jarvis.Application.Tools;
using Jarvis.Shared.Configuration;
using Jarvis.Shared.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Jarvis.Tools.Browser;

/// <summary>
/// Searches the web and returns real results in the chat.
/// Provider is chosen by settings: DuckDuckGo (default) | Google | Brave
/// </summary>
public class WebFetchSearchTool : ITool
{
    private readonly HttpClient _http;
    private readonly SearchSettings _searchSettings;

    public string Name => "web_search_results";
    public string Description => "Searches the internet and returns the top results so the AI can answer with real, up-to-date information.";

    public WebFetchSearchTool(IHttpClientFactory httpClientFactory, SearchSettings searchSettings)
    {
        _http = httpClientFactory.CreateClient("WebSearch");
        _searchSettings = searchSettings;
    }

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters =
        [
            new ToolParameter("query", "The search query to look up on the internet", "string", true),
            new ToolParameter("count", "Number of results to return (default: 5, max: 10)", "number", false)
        ]
    };

    public async Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var query = parameters.TryGetValue("query", out var q) ? q?.ToString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(query))
            return Result.Failure<ToolResult>("Search query is required.");

        var count = 5;
        if (parameters.TryGetValue("count", out var c) && c is not null)
            int.TryParse(c.ToString(), out count);
        count = Math.Clamp(count, 1, 10);

        var provider = _searchSettings.Provider ?? "DuckDuckGo";

        // Provider explicitamente escolhido pelo usuário
        if (provider == "Google" && !string.IsNullOrWhiteSpace(_searchSettings.GoogleApiKey) && !string.IsNullOrWhiteSpace(_searchSettings.GoogleSearchEngineId))
            return await GoogleSearchAsync(query, count, cancellationToken);

        if (provider == "Brave" && !string.IsNullOrWhiteSpace(_searchSettings.BraveApiKey))
            return await BraveSearchAsync(query, count, cancellationToken);

        // Para cotações financeiras usa API dedicada (mais precisa)
        var financialResult = await TryFinancialApiAsync(query, cancellationToken);
        if (financialResult is not null)
            return Result.Success(ToolResult.Ok(financialResult));

        // DuckDuckGo é o padrão (ou fallback quando o provider escolhido não está configurado)
        return await DuckDuckGoHtmlSearchAsync(query, cancellationToken);
    }

    // ── Google Custom Search API ─────────────────────────────────────────────
    private async Task<Result<ToolResult>> GoogleSearchAsync(string query, int count, CancellationToken ct)
    {
        try
        {
            var encoded = HttpUtility.UrlEncode(query);
            var url = $"https://www.googleapis.com/customsearch/v1?key={_searchSettings.GoogleApiKey}&cx={_searchSettings.GoogleSearchEngineId}&q={encoded}&num={Math.Min(count, 10)}&lr=lang_pt&gl=br";

            var json = await _http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("items", out var items))
                return Result.Success(ToolResult.Ok($"Nenhum resultado encontrado para \"{query}\"."));

            var results = items.EnumerateArray()
                .Select(r => new SearchResult(
                    r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    r.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "",
                    r.TryGetProperty("link", out var l) ? l.GetString() ?? "" : ""))
                .ToList();

            return Result.Success(ToolResult.Ok(FormatResults(query, results, "Google")));
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"Google Search error: {ex.Message}");
        }
    }

    // ── Brave Search API ─────────────────────────────────────────────────────
    private async Task<Result<ToolResult>> BraveSearchAsync(string query, int count, CancellationToken ct)
    {
        try
        {
            var encoded = HttpUtility.UrlEncode(query);
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.search.brave.com/res/v1/web/search?q={encoded}&count={count}");
            request.Headers.Add("X-Subscription-Token", _searchSettings.BraveApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var results = doc.RootElement
                .GetProperty("web").GetProperty("results")
                .EnumerateArray()
                .Select(r => new SearchResult(
                    r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    r.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    r.TryGetProperty("url", out var u) ? u.GetString() ?? "" : ""))
                .ToList();

            return Result.Success(ToolResult.Ok(FormatResults(query, results, "Brave Search")));
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"Brave Search error: {ex.Message}");
        }
    }

    // ── API de Câmbio gratuita (sem chave) ───────────────────────────────────
    private async Task<string?> TryFinancialApiAsync(string query, CancellationToken ct)
    {
        var lower = query.ToLowerInvariant();

        try
        {
            // Dólar → BRL
            if (lower.Contains("dolar") || lower.Contains("dólar") || lower.Contains("usd"))
            {
                var json = await _http.GetStringAsync("https://open.er-api.com/v6/latest/USD", ct);
                using var doc = JsonDocument.Parse(json);
                var rates = doc.RootElement.GetProperty("rates");
                var brl = rates.GetProperty("BRL").GetDouble();
                var updated = doc.RootElement.TryGetProperty("time_last_update_utc", out var upd)
                    ? upd.GetString() : "hoje";
                return $"Cotação atual do Dólar (USD):\n" +
                       $"1 USD = R$ {brl:F2} BRL\n" +
                       $"Atualizado em: {updated}\n\n" +
                       $"Fonte: open.er-api.com\n\n" +
                       $"Com base nesses dados, responda em português sobre o valor do dólar.";
            }

            // Euro → BRL
            if (lower.Contains("euro") || lower.Contains("eur"))
            {
                var json = await _http.GetStringAsync("https://open.er-api.com/v6/latest/EUR", ct);
                using var doc = JsonDocument.Parse(json);
                var brl = doc.RootElement.GetProperty("rates").GetProperty("BRL").GetDouble();
                return $"Cotação atual do Euro (EUR):\n1 EUR = R$ {brl:F2} BRL\nFonte: open.er-api.com\n\nResponda em português sobre o valor do euro.";
            }

            // Bitcoin
            if (lower.Contains("bitcoin") || lower.Contains("btc"))
            {
                var json = await _http.GetStringAsync("https://open.er-api.com/v6/latest/BTC", ct);
                using var doc = JsonDocument.Parse(json);
                var usd = doc.RootElement.GetProperty("rates").GetProperty("USD").GetDouble();
                var brl = doc.RootElement.GetProperty("rates").GetProperty("BRL").GetDouble();
                return $"Cotação atual do Bitcoin (BTC):\n1 BTC = ${usd:F2} USD / R$ {brl:F2} BRL\nFonte: open.er-api.com\n\nResponda em português sobre o valor do Bitcoin.";
            }
        }
        catch
        {
            // Se a API falhar, segue para busca normal
        }

        return null;
    }

    // ── DuckDuckGo HTML (resultados reais) ───────────────────────────────────
    private async Task<Result<ToolResult>> DuckDuckGoHtmlSearchAsync(string query, CancellationToken ct)
    {
        try
        {
            var encoded = HttpUtility.UrlEncode(query);
            var url = $"https://html.duckduckgo.com/html/?q={encoded}&kl=br-pt";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9");

            var html = await _http.GetStringAsync(url, ct);

            var results = ParseDuckDuckGoHtml(html);

            if (results.Count == 0)
                return Result.Success(ToolResult.Ok($"Nenhum resultado encontrado para \"{query}\"."));

            return Result.Success(ToolResult.Ok(FormatResults(query, results.Take(5).ToList(), "DuckDuckGo")));
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"Erro na busca: {ex.Message}");
        }
    }

    private static List<SearchResult> ParseDuckDuckGoHtml(string html)
    {
        var results = new List<SearchResult>();

        // Padrão 1: estrutura clássica result__a + result__snippet
        var pattern1 = new Regex(
            @"<a class=""result__a""[^>]*href=""([^""]+)""[^>]*>(.*?)</a>.*?<a class=""result__snippet""[^>]*>(.*?)</a>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match m in pattern1.Matches(html))
        {
            var url   = HttpUtility.HtmlDecode(m.Groups[1].Value.Trim());
            var title = HttpUtility.HtmlDecode(StripTags(m.Groups[2].Value)).Trim();
            var snip  = HttpUtility.HtmlDecode(StripTags(m.Groups[3].Value)).Trim();
            if (!string.IsNullOrEmpty(title))
                results.Add(new SearchResult(title, snip, url));
            if (results.Count >= 5) break;
        }

        if (results.Count > 0) return results;

        // Padrão 2: estrutura alternativa com data-domain e spans
        var pattern2 = new Regex(
            @"<h2[^>]*class=""[^""]*result__title[^""]*""[^>]*>.*?<a[^>]+href=""([^""]+)""[^>]*>(.*?)</a>.*?</h2>.*?<a[^>]*class=""[^""]*result__snippet[^""]*""[^>]*>(.*?)</a>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match m in pattern2.Matches(html))
        {
            var url   = HttpUtility.HtmlDecode(m.Groups[1].Value.Trim());
            var title = HttpUtility.HtmlDecode(StripTags(m.Groups[2].Value)).Trim();
            var snip  = HttpUtility.HtmlDecode(StripTags(m.Groups[3].Value)).Trim();
            if (!string.IsNullOrEmpty(title))
                results.Add(new SearchResult(title, snip, url));
            if (results.Count >= 5) break;
        }

        if (results.Count > 0) return results;

        // Padrão 3: qualquer link dentro de result com texto e snippet
        var pattern3 = new Regex(
            @"class=""result[^""]*"".*?<a[^>]+href=""(https?://[^""]+)""[^>]*>([^<]{10,})</a>.*?<span[^>]*>([^<]{20,})</span>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match m in pattern3.Matches(html))
        {
            var url   = HttpUtility.HtmlDecode(m.Groups[1].Value.Trim());
            var title = HttpUtility.HtmlDecode(StripTags(m.Groups[2].Value)).Trim();
            var snip  = HttpUtility.HtmlDecode(StripTags(m.Groups[3].Value)).Trim();
            if (!string.IsNullOrEmpty(title) && !url.Contains("duckduckgo.com"))
                results.Add(new SearchResult(title, snip, url));
            if (results.Count >= 5) break;
        }

        return results;
    }

    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", " ").Trim();

    // ── Formatação ────────────────────────────────────────────────────────────
    private static string FormatResults(string query, List<SearchResult> results, string source)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Resultados da busca para: \"{query}\" (via {source})");
        sb.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine($"[{i + 1}] {r.Title}");
            if (!string.IsNullOrEmpty(r.Description))
                sb.AppendLine($"    {r.Description}");
            if (!string.IsNullOrEmpty(r.Url))
                sb.AppendLine($"    Fonte: {r.Url}");
            sb.AppendLine();
        }

        sb.AppendLine("Com base nesses resultados, responda em português de forma clara e direta.");
        return sb.ToString();
    }

    private record SearchResult(string Title, string Description, string Url);
}
