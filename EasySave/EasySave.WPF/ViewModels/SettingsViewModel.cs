namespace EasySave.WPF.ViewModels;

using System.IO;
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
        BusinessSoftware = settings.BusinessSoftware;
        PriorityExtension = settings.PriorityExtension;
        LargeFileThresholdKB = settings.LargeFileThresholdKB;

        // Load log storage settings
        SelectedLogStorageModeIndex = (int)settings.LogStorageMode;
        LogServerIp = settings.LogServerIp;
        LogServerPort = settings.LogServerPort;
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

    private string _businessSoftware = string.Empty;
    public string BusinessSoftware
    {
        get => _businessSoftware;
        set => SetProperty(ref _businessSoftware, value);
    }

    private string _businessSoftwareLabel = string.Empty;
    public string BusinessSoftwareLabel
    {
        get => _businessSoftwareLabel;
        set => SetProperty(ref _businessSoftwareLabel, value);
    }

    // Priority extension(s) – semicolon-separated list of dot-prefixed extensions (e.g. ".exe;.pdf")
    private string _priorityExtension = string.Empty;
    public string PriorityExtension
    {
        get => _priorityExtension;
        set => SetProperty(ref _priorityExtension, value);
    }

    private string _priorityExtensionLabel = string.Empty;
    public string PriorityExtensionLabel
    {
        get => _priorityExtensionLabel;
        set => SetProperty(ref _priorityExtensionLabel, value);
    }

    // Validation error for PriorityExtension – empty string = no error
    private string _priorityExtensionError = string.Empty;
    public string PriorityExtensionError
    {
        get => _priorityExtensionError;
        set => SetProperty(ref _priorityExtensionError, value);
    }

    // Max file size (KB) above which only one concurrent transfer is allowed. 0 = disabled.
    private long _largeFileThresholdKB;
    public long LargeFileThresholdKB
    {
        get => _largeFileThresholdKB;
        set => SetProperty(ref _largeFileThresholdKB, value);
    }

    private string _largeFileThresholdLabel = string.Empty;
    public string LargeFileThresholdLabel
    {
        get => _largeFileThresholdLabel;
        set => SetProperty(ref _largeFileThresholdLabel, value);
    }

    private string _appearanceSectionTitle = string.Empty;
    public string AppearanceSectionTitle
    {
        get => _appearanceSectionTitle;
        set => SetProperty(ref _appearanceSectionTitle, value);
    }

    private string _darkModeLabel = string.Empty;
    public string DarkModeLabel
    {
        get => _darkModeLabel;
        set => SetProperty(ref _darkModeLabel, value);
    }

    private string _darkModeDescription = string.Empty;
    public string DarkModeDescription
    {
        get => _darkModeDescription;
        set => SetProperty(ref _darkModeDescription, value);
    }

    private string _languageLabel = string.Empty;
    public string LanguageLabel
    {
        get => _languageLabel;
        set => SetProperty(ref _languageLabel, value);
    }

    private string _interfaceLanguageLabel = string.Empty;
    public string InterfaceLanguageLabel
    {
        get => _interfaceLanguageLabel;
        set => SetProperty(ref _interfaceLanguageLabel, value);
    }

    private string _backupSectionTitle = string.Empty;
    public string BackupSectionTitle
    {
        get => _backupSectionTitle;
        set => SetProperty(ref _backupSectionTitle, value);
    }

    private string _logServerSectionTitle = string.Empty;
    public string LogServerSectionTitle
    {
        get => _logServerSectionTitle;
        set => SetProperty(ref _logServerSectionTitle, value);
    }

    private string _logStorageModeLabel = string.Empty;
    public string LogStorageModeLabel
    {
        get => _logStorageModeLabel;
        set => SetProperty(ref _logStorageModeLabel, value);
    }

    private string _serverIpLabel = string.Empty;
    public string ServerIpLabel
    {
        get => _serverIpLabel;
        set => SetProperty(ref _serverIpLabel, value);
    }

    private string _serverPortLabel = string.Empty;
    public string ServerPortLabel
    {
        get => _serverPortLabel;
        set => SetProperty(ref _serverPortLabel, value);
    }

    private string _priorityExtensionHint = string.Empty;
    public string PriorityExtensionHint
    {
        get => _priorityExtensionHint;
        set => SetProperty(ref _priorityExtensionHint, value);
    }

    private string _largeFileThresholdHint = string.Empty;
    public string LargeFileThresholdHint
    {
        get => _largeFileThresholdHint;
        set => SetProperty(ref _largeFileThresholdHint, value);
    }

    public string[] LogStorageModes { get; } =
    {
        "LocalOnly",
        "RemoteOnly",
        "LocalAndRemote"
    };

    private int _selectedLogStorageModeIndex;
    public int SelectedLogStorageModeIndex
    {
        get => _selectedLogStorageModeIndex;
        set => SetProperty(ref _selectedLogStorageModeIndex, value);
    }

    private string _logServerIp = "127.0.0.1";
    public string LogServerIp
    {
        get => _logServerIp;
        set => SetProperty(ref _logServerIp, value);
    }

    private int _logServerPort = 5000;
    public int LogServerPort
    {
        get => _logServerPort;
        set => SetProperty(ref _logServerPort, value);
    }

    // Save settings to config
    private void SaveSettings()
    {
        // Validate priority extension list before persisting
        if (!ValidatePriorityExtensionList(PriorityExtension, out string validationError))
        {
            PriorityExtensionError = validationError;
            MessageBox.Show(validationError, _localization.GetString("warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        PriorityExtensionError = string.Empty;

        var settings = _configManager.LoadSettings();
        settings.LogFormat = SelectedLogFormat;
        settings.ExtensionsToEncrypt = ExtensionsToEncrypt ?? string.Empty;
        settings.BusinessSoftware = BusinessSoftware ?? string.Empty;
        settings.PriorityExtension = PriorityExtension ?? string.Empty;
        settings.LargeFileThresholdKB = LargeFileThresholdKB;
        settings.LogStorageMode = (LogStorageMode)SelectedLogStorageModeIndex;
        settings.LogServerIp = LogServerIp;
        settings.LogServerPort = LogServerPort;

        _configManager.SaveSettings(settings);

        MessageBox.Show(_localization.GetString("settings_saved"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Validates a ';'-separated priority extension list.
    // Returns true when the input is empty (disabled) or every non-empty trimmed token starts with '.'
    // and contains no whitespace or path-separator characters.
    // Invalid tokens are NOT auto-corrected per spec (e.g. "exe" is rejected, not turned into ".exe").
    private bool ValidatePriorityExtensionList(string? input, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return true; // empty = priority disabled, always valid

        var tokens = input.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => t.Length > 0)
                          .ToList();

        if (tokens.Count == 0) return true; // only semicolons/spaces entered

        var invalidPathChars = Path.GetInvalidFileNameChars();
        foreach (var token in tokens)
        {
            bool isValid = token.StartsWith('.')
                           && token.Length >= 2
                           && !token.Any(char.IsWhiteSpace)
                           && token.IndexOfAny(invalidPathChars) < 0;
            if (!isValid)
            {
                error = _localization.GetString("priority_extension_invalid");
                return false;
            }
        }
        return true;
    }

    // Update localized strings when language changes
    public void UpdateLocalizedStrings()
    {
        GeneralSettingsTitle = _localization.GetString("general_settings");
        LogFormatLabel = _localization.GetString("log_format");
        ExtensionsToEncryptLabel = _localization.GetString("extensions_to_encrypt");
        BusinessSoftwareLabel = _localization.GetString("business_software");
        PriorityExtensionLabel = _localization.GetString("priority_extension");
        LargeFileThresholdLabel = _localization.GetString("large_file_threshold_kb");
        SaveSettingsText = _localization.GetString("save_settings");

        AppearanceSectionTitle = _localization.GetString("appearance");
        DarkModeLabel = _localization.GetString("dark_mode");
        DarkModeDescription = _localization.GetString("dark_mode_description");
        LanguageLabel = _localization.GetString("language");
        InterfaceLanguageLabel = _localization.GetString("interface_language");
        BackupSectionTitle = _localization.GetString("backup_section");
        LogServerSectionTitle = _localization.GetString("log_server");
        LogStorageModeLabel = _localization.GetString("log_storage_mode");
        ServerIpLabel = _localization.GetString("server_ip");
        ServerPortLabel = _localization.GetString("server_port");
        PriorityExtensionHint = _localization.GetString("priority_extension_hint");
        LargeFileThresholdHint = _localization.GetString("large_file_threshold_hint");
    }
}
