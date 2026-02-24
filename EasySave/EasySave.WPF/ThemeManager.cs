/*
 * ThemeManager: manages runtime Light/Dark theme switching by swapping the first merged
 * ResourceDictionary in Application.Resources (always the theme palette file).
 * All styles in Controls.xaml use DynamicResource, so they automatically update
 * when the dictionary at index 0 is replaced — no page reload required.
 */
using System;
using System.Linq;
using System.Windows;

namespace EasySave.WPF;

public static class ThemeManager
{
    private static bool _isDark = false;

    // Returns true when the active theme is Dark.
    public static bool IsDark => _isDark;

    // Applies the specified theme at runtime by replacing the palette dictionary at index 0.
    public static void ApplyTheme(bool dark)
    {
        _isDark = dark;

        var app = Application.Current;
        if (app == null) return;

        // Remove the existing theme dictionary (always at index 0).
        if (app.Resources.MergedDictionaries.Count > 0)
            app.Resources.MergedDictionaries.RemoveAt(0);

        string themeName = dark ? "Dark" : "Light";
        var themeUri = new Uri(
            $"/EasySave.WPF;component/Themes/{themeName}.xaml",
            UriKind.Relative);

        var newTheme = new ResourceDictionary { Source = themeUri };

        // Insert at index 0 so Controls.xaml (at index 1) resolves DynamicResource brushes from it.
        app.Resources.MergedDictionaries.Insert(0, newTheme);
    }

    // Toggles between Light and Dark themes.
    public static void Toggle() => ApplyTheme(!_isDark);
}
