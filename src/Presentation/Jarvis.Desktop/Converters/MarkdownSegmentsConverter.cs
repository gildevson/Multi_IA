using Jarvis.Desktop.Helpers;
using System.Globalization;
using System.Windows.Data;

namespace Jarvis.Desktop.Converters;

public class MarkdownSegmentsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string md ? MarkdownParser.Parse(md) : new List<MarkdownSegment>();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
