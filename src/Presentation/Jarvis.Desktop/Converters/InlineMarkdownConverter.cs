using Jarvis.Desktop.Helpers;
using System.Globalization;
using System.Windows.Data;

namespace Jarvis.Desktop.Converters;

public class InlineMarkdownConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string text ? InlineMarkdownParser.Parse(text).ToList() : new List<object>();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
