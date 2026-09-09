using System.Globalization;
using System.Windows.Data;

namespace Jarvis.Desktop.Converters;

public class LanguageToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString()?.ToLower() switch
        {
            "csharp" or "cs" or "c#" => "C#",
            "javascript" or "js" => "JavaScript",
            "typescript" or "ts" => "TypeScript",
            "python" or "py" => "Python",
            "sql" => "SQL",
            "json" => "JSON",
            "xml" => "XML",
            "html" => "HTML",
            "css" => "CSS",
            "bash" or "sh" => "Bash",
            "powershell" or "ps1" => "PowerShell",
            "java" => "Java",
            "cpp" or "c++" => "C++",
            var s when !string.IsNullOrEmpty(s) => s.ToUpper(),
            _ => "Code"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
