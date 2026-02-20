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

        InitializeDashboard();
    }

    private async void InitializeDashboard()
    {
        await UpdateLocalizedStringsAsync();
        await RefreshContentAsync();
    }

    // Rafraichir le contenu en fonction du settings de format de log
    public async Task RefreshContentAsync()
    {
        // Récupération état local
        var stateContent = _stateManager.ReadStateFileContent();
        StateContent = string.IsNullOrEmpty(stateContent)
            ? _localization.GetString("state_preview_placeholder")
            : stateContent;

        // Récupération logs
        string logContent;
        try
        {
            logContent = await _logger.ReadCurrentLogAsync();
        }
        catch
        {
            logContent = string.Empty;
        }

        var format = _logger.GetCurrentLogFormat();
        string placeholderKey = format == "xml"
            ? "log_preview_placeholder_xml"
            : "log_preview_placeholder_json";

        LogContent = string.IsNullOrEmpty(logContent)
            ? _localization.GetString(placeholderKey)
            : logContent;

        var formatUpper = format.ToUpper();
        LogFileTitle = $"{_localization.GetString("log_file_preview")} ({formatUpper})";
    }

    // Update localized strings when language changes or after settings update
    public async Task UpdateLocalizedStringsAsync()
    {
        DashboardTitle = _localization.GetString("dashboard");
        StateFileTitle = _localization.GetString("state_file_preview");

        var format = _logger.GetCurrentLogFormat().ToUpper();
        LogFileTitle = $"{_localization.GetString("log_file_preview")} ({format})";

        // Mettre à jour le contenu log
        string logContent;
        try
        {
            logContent = await _logger.ReadCurrentLogAsync();
        }
        catch
        {
            logContent = string.Empty;
        }

        string placeholderKey = format.ToLower() == "xml"
            ? "log_preview_placeholder_xml"
            : "log_preview_placeholder_json";

        LogContent = string.IsNullOrEmpty(logContent)
            ? _localization.GetString(placeholderKey)
            : logContent;
    }
}