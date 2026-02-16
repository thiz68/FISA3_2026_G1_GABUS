namespace EasySave.WPF.ViewModels;

using System.Windows;
using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.WPF.Commands;

// ViewModel for the Settings view
public class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly ConfigManager _configManager;

    // Settings properties
    private string _selectedLogFormat = "json";
    public string SelectedLogFormat
    {
        get => _selectedLogFormat;
        set => SetProperty(ref _selectedLogFormat, value);
    }

    // Available log formats
    public string[] LogFormats { get; } = { "JSON", "XML" };

    private int _selectedLogFormatIndex;
    public int SelectedLogFormatIndex
    {
        get => _selectedLogFormatIndex;
        set
        {
            if (SetProperty(ref _selectedLogFormatIndex, value))
            {
                SelectedLogFormat = value == 0 ? "json" : "xml";
            }
        }
    }

    // Commands
    public ICommand SaveSettingsCommand { get; }

    // Localized strings
    private string _generalSettingsTitle = string.Empty;
    public string GeneralSettingsTitle
    {
        get => _generalSettingsTitle;
        set => SetProperty(ref _generalSettingsTitle, value);
    }

    private string _logFormatLabel = string.Empty;
    public string LogFormatLabel
    {
        get => _logFormatLabel;
        set => SetProperty(ref _logFormatLabel, value);
    }

    private string _saveSettingsText = string.Empty;
    public string SaveSettingsText
    {
        get => _saveSettingsText;
        set => SetProperty(ref _saveSettingsText, value);
    }

    public SettingsViewModel(ILocalizationService localization, ConfigManager configManager)
    {
        _localization = localization;
        _configManager = configManager;

        // Initialize command
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());

        // Load current settings
        LoadSettings();

        UpdateLocalizedStrings();
    }

    // Load settings from config
    private void LoadSettings()
    {
        var settings = _configManager.LoadSettings();
        SelectedLogFormat = settings.LogFormat;
        SelectedLogFormatIndex = settings.LogFormat == "json" ? 0 : 1;
        
        ExtensionsToEncrypt = settings.ExtensionsToEncrypt;
    }
    
    // Properties + Label Encrypt Extensions
    private string _extensionsToEncrypt = string.Empty;
    public string ExtensionsToEncrypt
    {
        get => _extensionsToEncrypt;
        set => SetProperty(ref _extensionsToEncrypt, value);
    }

    private string _extensionsToEncryptLabel = string.Empty;
    public string ExtensionsToEncryptLabel
    {
        get => _extensionsToEncryptLabel;
        set => SetProperty(ref _extensionsToEncryptLabel, value);
    }

    // Save settings to config
    private void SaveSettings()
    {
        var settings = _configManager.LoadSettings();
        settings.LogFormat = SelectedLogFormat;
        settings.ExtensionsToEncrypt = ExtensionsToEncrypt ?? string.Empty;
        
        _configManager.SaveSettings(settings);


        MessageBox.Show(_localization.GetString("settings_saved"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Update localized strings when language changes
    public void UpdateLocalizedStrings()
    {
        GeneralSettingsTitle = _localization.GetString("general_settings");
        LogFormatLabel = _localization.GetString("log_format");
        ExtensionsToEncryptLabel = _localization.GetString("extensions_to_encrypt");
        SaveSettingsText = _localization.GetString("save_settings");
    }
}
