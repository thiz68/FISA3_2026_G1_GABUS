using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace EasySave.WPF.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml.
/// Code-behind is used ONLY for the theme toggle (allowed per design constraints).
/// All other logic lives in SettingsViewModel.
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        // Sync toggle button visual state to match current theme on load
        Loaded += (_, _) =>
        {
            if (ThemeToggle != null)
                ThemeToggle.IsChecked = ThemeManager.IsDark;
        };
    }

    /// <summary>
    /// Handles the theme toggle click: applies Light or Dark theme
    /// depending on the toggle's new checked state.
    /// </summary>
    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
            ThemeManager.ApplyTheme(toggle.IsChecked == true);
    }
}
