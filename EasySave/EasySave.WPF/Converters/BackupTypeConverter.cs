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

// Converts boolean to Visibility
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Windows.Visibility visibility)
        {
            return visibility == System.Windows.Visibility.Visible;
        }
        return false;
    }
}
