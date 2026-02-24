/*
 * MainViewModel: application root ViewModel; owns all shared services and child ViewModels.
 * Drives navigation by changing CurrentViewModel, which is bound to a ContentControl
 * in MainWindow.xaml via DataTemplates. Language changes are broadcast to all child
 * ViewModels via the ILocalizationService.LanguageChanged event.
 */
namespace EasySave.WPF.ViewModels;

using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Services;
using EasySave.WPF.Commands;
using EasySaveLog;

public class MainViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly IJobManager _jobManager;
    private readonly ConfigManager _configManager;
    private readonly BackupExecutor _backupExecutor;
    private readonly Logger _logger;
    private readonly StateManager _stateManager;
    private readonly PathValidator _pathValidator;
    private readonly CryptoSoftRunner _cryptoRunner;

    // Current view displayed in the main content area.
    private BaseViewModel _currentViewModel = null!;
    public BaseViewModel CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    // Child ViewModels
    public DashboardViewModel DashboardViewModel { get; }
    public JobsViewModel JobsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public ActivityViewModel ActivityViewModel { get; }

    // Navigation commands
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToJobsCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToActivityCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand SetLanguageFrCommand { get; }
    public ICommand SetLanguageEnCommand { get; }

    // Localized strings for UI binding
    private string _appTitle = string.Empty;
    public string AppTitle
    {
        get => _appTitle;
        set => SetProperty(ref _appTitle, value);
    }

    private string _dashboardText = string.Empty;
    public string DashboardText
    {
        get => _dashboardText;
        set => SetProperty(ref _dashboardText, value);
    }

    private string _backupJobsText = string.Empty;
    public string BackupJobsText
    {
        get => _backupJobsText;
        set => SetProperty(ref _backupJobsText, value);
    }

    private string _settingsText = string.Empty;
    public string SettingsText
    {
        get => _settingsText;
        set => SetProperty(ref _settingsText, value);
    }

    private string _activityText = string.Empty;
    public string ActivityText
    {
        get => _activityText;
        set => SetProperty(ref _activityText, value);
    }

    private string _exitText = string.Empty;
    public string ExitText
    {
        get => _exitText;
        set => SetProperty(ref _exitText, value);
    }

    public MainViewModel()
    {
        _localization = new LocalizationService();
        _jobManager = new JobManager(_localization);
        _configManager = new ConfigManager();
        _logger = new Logger(_configManager);
        _stateManager = new StateManager();
        _backupExecutor = new BackupExecutor(_localization);
        _pathValidator = new PathValidator();
        _cryptoRunner = new CryptoSoftRunner();

        _logger.Initialize();
        _logger.SetLogFormat(_configManager.LoadSettings().LogFormat);

        // Load existing jobs from config before child ViewModels are created.
        _configManager.LoadJobs(_jobManager);

        // Apply persisted language before the UI is shown.
        var settings = _configManager.LoadSettings();
        _localization.SetLanguage(settings.Language);

        // Broadcast language changes to all child ViewModels.
        _localization.LanguageChanged += OnLanguageChanged;

        DashboardViewModel = new DashboardViewModel(_localization, _stateManager, _logger, _configManager);
        Task.Run(async () => await DashboardViewModel.RefreshContentAsync());
        JobsViewModel = new JobsViewModel(_localization, _jobManager, _configManager, _backupExecutor, _logger, _stateManager, _pathValidator, _cryptoRunner);
        SettingsViewModel = new SettingsViewModel(_localization, _configManager);
        ActivityViewModel = new ActivityViewModel(_localization, _logger);

        NavigateToDashboardCommand = new RelayCommand(_ => NavigateToDashboard());
        NavigateToJobsCommand = new RelayCommand(_ => NavigateToJobs());
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateToSettings());
        NavigateToActivityCommand = new RelayCommand(_ => NavigateToActivity());
        ExitCommand = new RelayCommand(_ => ExitApplication());
        SetLanguageFrCommand = new RelayCommand(_ => SetLanguage("fr"));
        SetLanguageEnCommand = new RelayCommand(_ => SetLanguage("en"));

        CurrentViewModel = DashboardViewModel;
        UpdateLocalizedStrings();
    }

    private async void NavigateToDashboard()
    {
        await DashboardViewModel.RefreshContentAsync();
        CurrentViewModel = DashboardViewModel;
    }

    private void NavigateToJobs()
    {
        JobsViewModel.RefreshJobs();
        CurrentViewModel = JobsViewModel;
    }

    private void NavigateToSettings()
    {
        CurrentViewModel = SettingsViewModel;
    }

    private void NavigateToActivity()
    {
        _ = ActivityViewModel.LoadLogsAsync();
        CurrentViewModel = ActivityViewModel;
    }

    private void ExitApplication()
    {
        // Persist jobs before shutdown so no CRUD changes are lost.
        _configManager.SaveJobs(_jobManager);
        System.Windows.Application.Current.Shutdown();
    }

    private void SetLanguage(string languageCode)
    {
        _localization.SetLanguage(languageCode);

        var settings = _configManager.LoadSettings();
        settings.Language = languageCode;
        _configManager.SaveSettings(settings);
    }

    // Called when language changes: re-localizes this ViewModel and all child ViewModels.
    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalizedStrings();
        await DashboardViewModel.UpdateLocalizedStringsAsync();
        JobsViewModel.UpdateLocalizedStrings();
        SettingsViewModel.UpdateLocalizedStrings();
        ActivityViewModel.UpdateLocalizedStrings();
    }

    private void UpdateLocalizedStrings()
    {
        AppTitle = _localization.GetString("app_title");
        DashboardText = _localization.GetString("dashboard");
        BackupJobsText = _localization.GetString("backup_jobs");
        SettingsText = _localization.GetString("settings");
        ActivityText = _localization.GetString("activity");
        ExitText = _localization.GetString("exit");
    }

}
