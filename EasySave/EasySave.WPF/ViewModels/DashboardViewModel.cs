namespace EasySave.WPF.ViewModels;

using EasySave.Core.Interfaces;
using EasySave.Core.Services;
using EasySaveLog;


// ViewModel for the Dashboard view
// Displays real-time state and log file previews
public class DashboardViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly StateManager _stateManager;
    private readonly Logger _logger;
    private readonly ConfigManager _configManager;

    // Localized strings
    private string _dashboardTitle = string.Empty;
    public string DashboardTitle
    {
        get => _dashboardTitle;
        set => SetProperty(ref _dashboardTitle, value);
    }

    private string _stateFileTitle = string.Empty;
    public string StateFileTitle
    {
        get => _stateFileTitle;
        set => SetProperty(ref _stateFileTitle, value);
    }

    private string _logFileTitle = string.Empty;
    public string LogFileTitle
    {
        get => _logFileTitle;
        set => SetProperty(ref _logFileTitle, value);
    }

    // Content for state and log previews
    private string _stateContent = string.Empty;
    public string StateContent
    {
        get => _stateContent;
        set => SetProperty(ref _stateContent, value);
    }

    private string _logContent = string.Empty;
    public string LogContent
    {
        get => _logContent;
        set => SetProperty(ref _logContent, value);
    }

    public DashboardViewModel(ILocalizationService localization, StateManager stateManager, Logger logger, ConfigManager configManager)
    {
        _localization = localization;
        _stateManager = stateManager;
        _logger = logger;
        _configManager = configManager;

        UpdateLocalizedStrings();
        RefreshContent();
    }

    // Refresh state and log file content
    public void RefreshContent()
    {
        var stateContent = _stateManager.ReadStateFileContent();
        StateContent = string.IsNullOrEmpty(stateContent)
            ? _localization.GetString("state_preview_placeholder")
            : stateContent;

        var logContent = _logger.ReadLogFileContent();
        LogContent = string.IsNullOrEmpty(logContent)
            ? (_configManager.LoadSettings().LogFormat == "json" 
            ? _localization.GetString("log_preview_placeholder") : _localization.GetString("log_preview_placeholder_xml"))
            : logContent;
    }

    // Update localized strings when language changes
    public void UpdateLocalizedStrings()
    {
        DashboardTitle = _localization.GetString("dashboard");
        StateFileTitle = _localization.GetString("state_file_preview");
        LogFileTitle = _localization.GetString("log_file_preview");

        // Update placeholders if content is empty
        if (string.IsNullOrEmpty(_stateManager.ReadStateFileContent()))
        {
            StateContent = _localization.GetString("state_preview_placeholder");
        }
        if (string.IsNullOrEmpty(_logger.ReadLogFileContent()))
        {
            var logContent = _logger.ReadLogFileContent();
            LogContent = string.IsNullOrEmpty(logContent)
            ? (_configManager.LoadSettings().LogFormat == "json"
            ? _localization.GetString("log_preview_placeholder") : _localization.GetString("log_preview_placeholder_xml"))
            : logContent;
        }
    }
}
