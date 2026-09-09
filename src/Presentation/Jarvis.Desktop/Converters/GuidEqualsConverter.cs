using System.Globalization;
using System.Windows.Data;

namespace Jarvis.Desktop.Converters;

public class GuidEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        if (values[0] is Guid a && values[1] is Guid b)
            return a == b;
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
