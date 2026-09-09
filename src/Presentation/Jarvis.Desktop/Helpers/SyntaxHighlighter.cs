using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace Jarvis.Desktop.Helpers;

public static class SyntaxHighlighter
{
    // Dracula palette
    private static readonly SolidColorBrush ColorKeyword  = Brush("#ff79c6"); // pink  — keywords
    private static readonly SolidColorBrush ColorType     = Brush("#8be9fd"); // cyan  — types / class names
    private static readonly SolidColorBrush ColorString   = Brush("#f1fa8c"); // yellow — strings
    private static readonly SolidColorBrush ColorComment  = Brush("#6272a4"); // grey-blue — comments
    private static readonly SolidColorBrush ColorNumber   = Brush("#bd93f9"); // purple — numbers
    private static readonly SolidColorBrush ColorOperator = Brush("#ff79c6"); // pink  — operators
    private static readonly SolidColorBrush ColorDefault  = Brush("#f8f8f2"); // white — normal text

    private static SolidColorBrush Brush(string hex)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex);
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    // Token pattern (order matters — evaluated left-to-right)
    private static readonly Regex TokenRegex = new(
        @"(""(?:[^""\\]|\\.)*""" +          // double-quoted string
        @"|'(?:[^'\\]|\\.)*'"   +            // single-quoted string / char
        @"|//[^\n]*"            +            // line comment
        @"|/\*[\s\S]*?\*/"      +            // block comment
        @"|\b\d+(?:\.\d+)?(?:[fFdDmMlLuU]*)?\b" + // number
        @"|\b[A-Za-z_]\w*\b"   +            // identifier / keyword
        @"|[^\w\s""'/]+"        +            // operators / punctuation
        @"|\s+)",                            // whitespace
        RegexOptions.Compiled);

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked",
        "class","const","continue","decimal","default","delegate","do","double","else",
        "enum","event","explicit","extern","false","finally","fixed","float","for",
        "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params",
        "private","protected","public","readonly","ref","return","sbyte","sealed",
        "short","sizeof","stackalloc","static","string","struct","switch","this","throw",
        "true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using",
        "virtual","void","volatile","while","var","async","await","get","set","value",
        "init","record","with","required","file","scoped","nint","nuint","not","and","or"
    };

    private static readonly HashSet<string> JavaKeywords = new(StringComparer.Ordinal)
    {
        "abstract","assert","boolean","break","byte","case","catch","char","class","const",
        "continue","default","do","double","else","enum","extends","final","finally","float",
        "for","goto","if","implements","import","instanceof","int","interface","long","native",
        "new","null","package","private","protected","public","return","short","static",
        "strictfp","super","switch","synchronized","this","throw","throws","transient","true",
        "try","void","volatile","while","var","yield","record","sealed","permits"
    };

    private static readonly HashSet<string> PythonKeywords = new(StringComparer.Ordinal)
    {
        "False","None","True","and","as","assert","async","await","break","class","continue",
        "def","del","elif","else","except","finally","for","from","global","if","import",
        "in","is","lambda","nonlocal","not","or","pass","raise","return","try","while",
        "with","yield","self","cls","print","range","len","type","int","str","float","bool","list","dict","tuple","set"
    };

    private static readonly HashSet<string> JsKeywords = new(StringComparer.Ordinal)
    {
        "break","case","catch","class","const","continue","debugger","default","delete","do",
        "else","export","extends","false","finally","for","function","if","import","in",
        "instanceof","let","new","null","return","static","super","switch","this","throw",
        "true","try","typeof","undefined","var","void","while","with","yield","async","await",
        "of","from","interface","type","enum","implements","private","protected","public","abstract","readonly"
    };

    public static IEnumerable<Run> Highlight(string code, string language)
    {
        if (string.IsNullOrEmpty(code))
            yield break;

        var keywords = GetKeywords(language);

        foreach (Match m in TokenRegex.Matches(code))
        {
            var token = m.Value;
            var run = new Run(token)
            {
                Foreground = GetColor(token, keywords)
            };
            yield return run;
        }
    }

    private static SolidColorBrush GetColor(string token, HashSet<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ColorDefault;

        // strings
        if (token.StartsWith('"') || token.StartsWith('\''))
            return ColorString;

        // comments
        if (token.StartsWith("//") || token.StartsWith("/*"))
            return ColorComment;

        // numbers
        if (char.IsDigit(token[0]))
            return ColorNumber;

        // keywords
        if (keywords.Contains(token))
            return ColorKeyword;

        // Types (PascalCase)
        if (char.IsUpper(token[0]) && token.Length > 1 && char.IsLetter(token[1]))
            return ColorType;

        // operators / punctuation (non-alphanumeric, non-space)
        if (!char.IsLetterOrDigit(token[0]) && token[0] != '_' && token[0] != ' ' && token[0] != '\n' && token[0] != '\t')
            return ColorOperator;

        return ColorDefault;
    }

    private static HashSet<string> GetKeywords(string language) => language.ToLower() switch
    {
        "csharp" or "cs" or "c#" => CSharpKeywords,
        "java"                    => JavaKeywords,
        "python" or "py"          => PythonKeywords,
        "javascript" or "js"
            or "typescript" or "ts" => JsKeywords,
        _                         => CSharpKeywords
    };
}
