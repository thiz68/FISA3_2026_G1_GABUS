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

    private const string LightSource = "Resources/Theme/Colors.Light.xaml";
    private const string DarkSource  = "Resources/Theme/Colors.Dark.xaml";

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static void Apply(AppTheme theme)
    {
        if (Application.Current?.Resources?.MergedDictionaries == null)
            return;

        var merged = Application.Current.Resources.MergedDictionaries;

        // Retire le dictionnaire Colors.* existant (Light ou Dark)
        var existing = merged.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.EndsWith("Colors.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
             d.Source.OriginalString.EndsWith("Colors.Dark.xaml", StringComparison.OrdinalIgnoreCase)));

        if (existing != null)
            merged.Remove(existing);

        // Ajoute le nouveau dictionnaire Colors.* en 1er (important: les Brushes dépendent des Colors)
        var source = theme == AppTheme.Dark ? DarkSource : LightSource;
        merged.Insert(0, new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });

        CurrentTheme = theme;
    }

    public static void Toggle()
    {
        Apply(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }
}