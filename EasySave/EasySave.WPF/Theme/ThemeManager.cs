using System;
using System.Linq;
using System.Windows;

namespace EasySave.WPF.Theme;

public static class ThemeManager
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    private const string LightSource = "Resources/Theme/Theme.Light.xaml";
    private const string DarkSource  = "Resources/Theme/Theme.Dark.xaml";

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static void Apply(AppTheme theme)
    {
        if (Application.Current?.Resources?.MergedDictionaries == null)
            return;

        var merged = Application.Current.Resources.MergedDictionaries;

        // Remove current Theme.* dictionary if present
        var existing = merged.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.EndsWith("Theme.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
             d.Source.OriginalString.EndsWith("Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase)));

        if (existing != null)
            merged.Remove(existing);

        // Insert new theme dictionary at index 0 (important: other dictionaries depend on these keys)
        var source = theme == AppTheme.Dark ? DarkSource : LightSource;
        merged.Insert(0, new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });

        CurrentTheme = theme;
    }

    public static void Toggle()
    {
        Apply(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }
}