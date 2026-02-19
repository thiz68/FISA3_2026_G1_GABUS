namespace EasySave.WPF.ViewModels;

using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Services;
using EasySave.WPF.Commands;
using EasySaveLog;
using EasySave.WPF.Theme; // <-- AJOUT

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

    private BaseViewModel _currentViewModel = null!;
    public BaseViewModel CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public DashboardViewModel DashboardViewModel { get; }
    public JobsViewModel JobsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToJobsCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand SetLanguageFrCommand { get; }
    public ICommand SetLanguageEnCommand { get; }

    // --- AJOUT : thème ---
    public ICommand ToggleThemeCommand { get; }

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                ThemeToggleText = _isDarkMode ? "Light" : "Dark";
            }
        }
    }

    private string _themeToggleText = "Dark";
    public string ThemeToggleText
    {
        get => _themeToggleText;
        set => SetProperty(ref _themeToggleText, value);
    }
    // --- fin AJOUT ---

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

        _configManager.LoadJobs(_jobManager);

        var settings = _configManager.LoadSettings();
        _localization.SetLanguage(settings.Language);

        _localization.LanguageChanged += OnLanguageChanged;

        DashboardViewModel = new DashboardViewModel(_localization, _stateManager, _logger, _configManager);
        JobsViewModel = new JobsViewModel(_localization, _jobManager, _configManager, _backupExecutor, _logger, _stateManager, _pathValidator, _cryptoRunner);
        SettingsViewModel = new SettingsViewModel(_localization, _configManager);

        NavigateToDashboardCommand = new RelayCommand(_ => NavigateToDashboard());
        NavigateToJobsCommand = new RelayCommand(_ => NavigateToJobs());
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateToSettings());
        ExitCommand = new RelayCommand(_ => ExitApplication());
        SetLanguageFrCommand = new RelayCommand(_ => SetLanguage("fr"));
        SetLanguageEnCommand = new RelayCommand(_ => SetLanguage("en"));

        // --- AJOUT : commande thème + thème initial ---
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        IsDarkMode = false;
        ThemeManager.Apply(ThemeManager.AppTheme.Light);
        // --- fin AJOUT ---

        CurrentViewModel = DashboardViewModel;

        UpdateLocalizedStrings();
    }

    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        ThemeManager.Apply(IsDarkMode ? ThemeManager.AppTheme.Dark : ThemeManager.AppTheme.Light);
    }

    private void NavigateToDashboard()
    {
        DashboardViewModel.RefreshContent();
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

    private void ExitApplication()
    {
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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalizedStrings();
        DashboardViewModel.UpdateLocalizedStrings();
        JobsViewModel.UpdateLocalizedStrings();
        SettingsViewModel.UpdateLocalizedStrings();
    }

    private void UpdateLocalizedStrings()
    {
        AppTitle = _localization.GetString("app_title");
        DashboardText = _localization.GetString("dashboard");
        BackupJobsText = _localization.GetString("backup_jobs");
        SettingsText = _localization.GetString("settings");
        ExitText = _localization.GetString("exit");
    }
}
