using System;
using System.Linq;
using System.Windows;

namespace EasySave.WPF;

/// <summary>
/// Manages runtime Light/Dark theme switching by swapping the first
/// merged ResourceDictionary in Application.Resources (the theme file).
/// Styles in Styles/Controls.xaml use DynamicResource and automatically
/// pick up the new brushes when the theme dictionary is replaced.
/// </summary>
public static class ThemeManager
{
    private static bool _isDark = false;

    /// <summary>Gets whether the current theme is Dark.</summary>
    public static bool IsDark => _isDark;

    /// <summary>
    /// Applies the specified theme (Light or Dark) at runtime.
    /// </summary>
    public static void ApplyTheme(bool dark)
    {
        _isDark = dark;

        var app = Application.Current;
        if (app == null) return;

        // Remove the existing theme dictionary (always at index 0)
        if (app.Resources.MergedDictionaries.Count > 0)
            app.Resources.MergedDictionaries.RemoveAt(0);

        // Build the new theme's pack URI
        string themeName = dark ? "Dark" : "Light";
        var themeUri = new Uri(
            $"/EasySave.WPF;component/Themes/{themeName}.xaml",
            UriKind.Relative);

        var newTheme = new ResourceDictionary { Source = themeUri };

        // Insert at position 0 so Controls.xaml (at index 1) resolves brushes from it
        app.Resources.MergedDictionaries.Insert(0, newTheme);
    }

    /// <summary>Toggles between Light and Dark themes.</summary>
    public static void Toggle() => ApplyTheme(!_isDark);
}
