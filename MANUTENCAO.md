# Jarvis — Guia de Manutenção

> Documentação técnica para manutenção e evolução do projeto.

---

## Estrutura da Solução

```
Jarvis/
├── src/
│   ├── Core/
│   │   ├── Jarvis.Application      → Comandos, queries, orquestração (CQRS/MediatR)
│   │   ├── Jarvis.Domain           → Entidades, enums, regras de negócio
│   │   └── Jarvis.Shared           → Modelos compartilhados (Result, SearchSettings...)
│   ├── Infrastructure/
│   │   └── Jarvis.Infrastructure   → Configurações (JarvisSettings, appsettings)
│   ├── Presentation/
│   │   └── Jarvis.Desktop          → Interface WPF (Views, ViewModels, Converters)
│   ├── Tools/
│   │   └── Jarvis.Tools            → Ferramentas do assistente (busca, sistema, browser...)
│   ├── AI/
│   │   └── Jarvis.AI               → Provedores de IA (Ollama, OpenAI, Claude, Gemini)
│   ├── Vision/
│   │   └── Jarvis.Vision           → Análise de imagens
│   └── Voice/
│       └── Jarvis.Voice            → STT (fala→texto) e TTS (texto→fala)
```

---

## Fluxo de uma Mensagem

```
Usuário digita
      ↓
ChatViewModel.SendMessageAsync()
      ↓
NeedsWebSearch()? → sim → WebFetchSearchTool.ExecuteAsync()
                              ↓
                         DuckDuckGo / Brave / Google / API Financeira
                              ↓
                         Resultado injetado no prompt (AiContextPrompt)
      ↓
MediatR → SendMessageCommand
      ↓
AssistantOrchestrator.ProcessUserInputAsync()
      ↓
IAIProvider.SendAsync() → Ollama / OpenAI / Claude / Gemini
      ↓
Resposta exibida no chat
```

---

## Sistema de Busca na Internet

### Como funciona

O Jarvis detecta automaticamente quando a pergunta precisa de dados atuais (notícias, cotações, etc.) e busca na internet **antes** de chamar a IA. O resultado é injetado no prompt como contexto.

### Prioridade de provedores

| Prioridade | Provedor | Configuração |
|---|---|---|
| 1 | Google Custom Search | API Key + CX em `appsettings.user.json` |
| 2 | Brave Search | API Key em `appsettings.user.json` |
| 3 | API Financeira | Automático (grátis, sem chave) — só câmbio |
| 4 | DuckDuckGo HTML | Automático (grátis, sem chave) — busca geral |

### Configurar via UI

`Configurações → aba 🔍 Busca` → selecionar provedor e inserir chaves → Salvar → **Reiniciar o Jarvis**

### Configurar via arquivo

Arquivo: `appsettings.user.json` (na pasta do executável)

```json
{
  "JarvisSettings": {
    "Search": {
      "Provider": "DuckDuckGo",
      "GoogleApiKey": "",
      "GoogleSearchEngineId": "",
      "BraveApiKey": ""
    }
  }
}
```

---

## Manutenção da Busca DuckDuckGo

### Sintoma de que quebrou

O Jarvis começa a responder perguntas sobre notícias/cotações sem dados atuais, dizendo que não tem informação.

### Como diagnosticar

1. Abra no navegador: `https://html.duckduckgo.com/html/?q=teste`
2. Pressione **Ctrl+U** (ver código fonte)
3. Pressione **Ctrl+F** e pesquise por: `result__a`, `result__title`, `result__snippet`
4. Veja qual `class` está nos links de resultado

### Como corrigir

Arquivo: `src/Tools/Jarvis.Tools/Browser/WebFetchSearchTool.cs`
Método: `ParseDuckDuckGoHtml(string html)`

Adicione um novo padrão seguindo o modelo abaixo:

```csharp
// Padrão N: descrição da nova estrutura
var patternN = new Regex(
    @"<a class=""NOVA_CLASSE""[^>]*href=""([^""]+)""[^>]*>(.*?)</a>.*?<span class=""NOVA_CLASSE_SNIPPET""[^>]*>(.*?)</span>",
    RegexOptions.Singleline | RegexOptions.IgnoreCase);

foreach (Match m in patternN.Matches(html))
{
    var url   = HttpUtility.HtmlDecode(m.Groups[1].Value.Trim());
    var title = HttpUtility.HtmlDecode(StripTags(m.Groups[2].Value)).Trim();
    var snip  = HttpUtility.HtmlDecode(StripTags(m.Groups[3].Value)).Trim();
    if (!string.IsNullOrEmpty(title))
        results.Add(new SearchResult(title, snip, url));
    if (results.Count >= 5) break;
}

if (results.Count > 0) return results;
```

> Substitua `NOVA_CLASSE` e `NOVA_CLASSE_SNIPPET` pelas classes encontradas no HTML atual.

---

## Como Adicionar uma Nova Ferramenta (Tool)

1. Crie uma classe em `src/Tools/Jarvis.Tools/` implementando `ITool`:

```csharp
public class MinhaFerramenta : ITool
{
    public string Name => "minha_ferramenta";
    public string Description => "Descrição do que faz.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = [
            new ToolParameter("param1", "Descrição", "string", true)
        ]
    };

    public async Task<Result<ToolResult>> ExecuteAsync(
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var valor = parameters["param1"]?.ToString() ?? "";
        // lógica aqui
        return Result.Success(ToolResult.Ok("resultado"));
    }
}
```

2. Registre em `src/Tools/Jarvis.Tools/DependencyInjection/ToolsServiceExtensions.cs`:

```csharp
services.AddSingleton<ITool, MinhaFerramenta>();
```

3. O sistema registra automaticamente no `ToolRegistry` — sem mais configuração.

---

## Como Adicionar um Novo Provedor de IA

1. Implemente `IAIProvider` em `src/AI/Jarvis.AI/`
2. Adicione o nome do provedor em `JarvisSettings.AIProvider` (lista de valores válidos)
3. Registre no DI em `AIServiceExtensions.cs`
4. Adicione a opção no ComboBox da aba **🤖 IA** nas configurações

---

## Palavras-chave que Disparam Busca Automática

Arquivo: `src/Presentation/Jarvis.Desktop/ViewModels/ChatViewModel.cs`
Método: `NeedsWebSearch(string text)`

Para adicionar novas palavras que disparam busca:

```csharp
string[] keywords =
[
    "hoje", "agora", "noticia", "banco", "dolar",
    // → adicione aqui
    "nova_palavra"
];
```

---

## Arquivos de Configuração

| Arquivo | Local | Finalidade |
|---|---|---|
| `appsettings.json` | Pasta do executável | Configurações padrão (não editar) |
| `appsettings.user.json` | Pasta do executável | Configurações do usuário (gerado ao salvar) |

---

## Problemas Comuns

| Sintoma | Causa Provável | Solução |
|---|---|---|
| Busca não traz resultados | DuckDuckGo mudou HTML | Atualizar regex em `ParseDuckDuckGoHtml` |
| IA não responde | Ollama não está rodando | Iniciar Ollama: `ollama serve` |
| Configurações não salvam | Permissão na pasta | Executar como administrador |
| Voz não funciona | Nenhuma voz instalada no Windows | Painel de Controle → Fala → Instalar vozes |
| Chaves de API não aplicam | Singleton criado antes de salvar | Reiniciar o Jarvis após salvar chaves |
