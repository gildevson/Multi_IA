using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Jarvis.Desktop.Converters;

public class MessageRoleToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var role = value?.ToString() ?? string.Empty;
        return role switch
        {
            "Assistant" => new SolidColorBrush(Color.FromRgb(22, 33, 62)),
            "Tool"      => new SolidColorBrush(Color.FromRgb(13, 27, 42)),
            _           => new SolidColorBrush(Color.FromRgb(15, 52, 96)) // user (qualquer nome)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class MessageRoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var role = value?.ToString() ?? string.Empty;
        return role is "Assistant" or "Tool"
            ? System.Windows.HorizontalAlignment.Left
            : System.Windows.HorizontalAlignment.Right; // user (qualquer nome)
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
