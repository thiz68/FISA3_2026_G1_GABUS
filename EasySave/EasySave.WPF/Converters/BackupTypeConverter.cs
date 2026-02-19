namespace EasySave.WPF.Converters;

using System.Globalization;
using System.Windows.Data;

// Converts backup type string to boolean for RadioButton binding
public class BackupTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string type && parameter is string paramType)
        {
            return type.Equals(paramType, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramType)
        {
            return paramType;
        }
        return Binding.DoNothing;
    }
}
