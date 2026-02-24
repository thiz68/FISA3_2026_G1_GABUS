/*
 * JobsViewModel: manages the backup job list and drives job execution.
 * Execution is handled on a background thread via BackupExecutor.ExecuteWithProgress.
 * Two pause signals are composed at runtime:
 *   - Business software pause (automatic): detected by polling every 500 ms.
 *   - Manual pause (user-initiated): toggled per-job via BackupProgressItemViewModel.
 * Business software has priority: when it is running, the Resume button is disabled.
 * A lock-protected flag (pausePopupShown) ensures the warning popup appears at most
 * once per false→true pause transition, regardless of how many jobs are running.
 */
namespace EasySave.WPF.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.WPF.Commands;
using EasySave.WPF.Views;
using EasySaveLog;

// ViewModel for a single job item in the list.
public class JobItemViewModel : BaseViewModel
{
    private readonly IJob _job;
    private readonly ILocalizationService _localization;

    public string Name => _job.Name;
    public string SourcePath => _job.SourcePath;
    public string TargetPath => _job.TargetPath;

    private string _typeDisplay = string.Empty;
    public string TypeDisplay
    {
        get => _typeDisplay;
        set => SetProperty(ref _typeDisplay, value);
    }

    public string Type => _job.Type;

    // Used for multi-selection in the DataGrid.
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public IJob Job => _job;

    public JobItemViewModel(IJob job, ILocalizationService localization)
    {
        _job = job;
        _localization = localization;
        UpdateTypeDisplay();
    }

    public void UpdateTypeDisplay()
    {
        TypeDisplay = _job.Type == "full"
            ? _localization.GetString("full")
            : _localization.GetString("diff");
    }
}

public class JobsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly IJobManager _jobManager;
    private readonly ConfigManager _configManager;
    private readonly BackupExecutor _backupExecutor;
    private readonly Logger _logger;
    private readonly StateManager _stateManager;
    private readonly PathValidator _pathValidator;
    private readonly CryptoSoftRunner _cryptoRunner;
    private readonly BusinessSoftwareChecker _businessChecker = new();

    // Observable collection of jobs for DataGrid binding.
    public ObservableCollection<JobItemViewModel> Jobs { get; } = new();

    private JobItemViewModel? _selectedJob;
    public JobItemViewModel? SelectedJob
    {
        get => _selectedJob;
        set => SetProperty(ref _selectedJob, value);
    }

    public ICommand AddJobCommand { get; }
    public ICommand ExecuteAllCommand { get; }
    public ICommand ExecuteSelectedCommand { get; }
    public ICommand DeleteJobCommand { get; }
    public ICommand EditJobCommand { get; }

    // Localized strings
    private string _addJobText = string.Empty;
    public string AddJobText
    {
        get => _addJobText;
        set => SetProperty(ref _addJobText, value);
    }

    private string _executeAllText = string.Empty;
    public string ExecuteAllText
    {
        get => _executeAllText;
        set => SetProperty(ref _executeAllText, value);
    }

    private string _executeSelectedText = string.Empty;
    public string ExecuteSelectedText
    {
        get => _executeSelectedText;
        set => SetProperty(ref _executeSelectedText, value);
    }

    private string _nameHeader = string.Empty;
    public string NameHeader
    {
        get => _nameHeader;
        set => SetProperty(ref _nameHeader, value);
    }

    private string _sourceHeader = string.Empty;
    public string SourceHeader
    {
        get => _sourceHeader;
        set => SetProperty(ref _sourceHeader, value);
    }

    private string _targetHeader = string.Empty;
    public string TargetHeader
    {
        get => _targetHeader;
        set => SetProperty(ref _targetHeader, value);
    }

    private string _typeHeader = string.Empty;
    public string TypeHeader
    {
        get => _typeHeader;
        set => SetProperty(ref _typeHeader, value);
    }

    private string _actionsHeader = string.Empty;
    public string ActionsHeader
    {
        get => _actionsHeader;
        set => SetProperty(ref _actionsHeader, value);
    }

    private string _deleteText = string.Empty;
    public string DeleteText
    {
        get => _deleteText;
        set => SetProperty(ref _deleteText, value);
    }

    private string _editText = string.Empty;
    public string EditText
    {
        get => _editText;
        set => SetProperty(ref _editText, value);
    }

    private string _browseText = string.Empty;
    public string BrowseText
    {
        get => _browseText;
        set => SetProperty(ref _browseText, value);
    }

    private string _fullText = string.Empty;
    public string FullText
    {
        get => _fullText;
        set => SetProperty(ref _fullText, value);
    }

    private string _diffText = string.Empty;
    public string DiffText
    {
        get => _diffText;
        set => SetProperty(ref _diffText, value);
    }

    private string _cancelText = string.Empty;
    public string CancelText
    {
        get => _cancelText;
        set => SetProperty(ref _cancelText, value);
    }

    private string _saveText = string.Empty;
    public string SaveText
    {
        get => _saveText;
        set => SetProperty(ref _saveText, value);
    }

    // Add/Edit dialog properties
    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    private string _dialogJobName = string.Empty;
    public string DialogJobName
    {
        get => _dialogJobName;
        set => SetProperty(ref _dialogJobName, value);
    }

    private string _dialogSourcePath = string.Empty;
    public string DialogSourcePath
    {
        get => _dialogSourcePath;
        set => SetProperty(ref _dialogSourcePath, value);
    }

    private string _dialogTargetPath = string.Empty;
    public string DialogTargetPath
    {
        get => _dialogTargetPath;
        set => SetProperty(ref _dialogTargetPath, value);
    }

    private string _dialogType = "full";
    public string DialogType
    {
        get => _dialogType;
        set => SetProperty(ref _dialogType, value);
    }

    private bool _isEditMode;
    private IJob? _editingJob;

    public ICommand SaveJobCommand { get; }
    public ICommand CancelDialogCommand { get; }

    public JobsViewModel(ILocalizationService localization, IJobManager jobManager, ConfigManager configManager,
        BackupExecutor backupExecutor, Logger logger, StateManager stateManager, PathValidator pathValidator, CryptoSoftRunner cryptoRunner)
    {
        _localization = localization;
        _jobManager = jobManager;
        _configManager = configManager;
        _backupExecutor = backupExecutor;
        _logger = logger;
        _stateManager = stateManager;
        _pathValidator = pathValidator;
        _cryptoRunner = cryptoRunner;

        AddJobCommand = new RelayCommand(_ => OpenAddDialog());
        ExecuteAllCommand = new RelayCommand(_ => ExecuteAll(), _ => Jobs.Count > 0);
        ExecuteSelectedCommand = new RelayCommand(_ => ExecuteSelected(), _ => Jobs.Any(j => j.IsSelected));
        DeleteJobCommand = new RelayCommand(param => DeleteJob(param as JobItemViewModel));
        EditJobCommand = new RelayCommand(param => OpenEditDialog(param as JobItemViewModel));
        SaveJobCommand = new RelayCommand(_ => SaveJob());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());

        UpdateLocalizedStrings();
        RefreshJobs();
    }

    public void RefreshJobs()
    {
        Jobs.Clear();
        foreach (var job in _jobManager.Jobs)
        {
            Jobs.Add(new JobItemViewModel(job, _localization));
        }
    }

    private void OpenAddDialog()
    {
        _isEditMode = false;
        _editingJob = null;
        DialogJobName = string.Empty;
        DialogSourcePath = string.Empty;
        DialogTargetPath = string.Empty;
        DialogType = "full";
        IsDialogOpen = true;
    }

    private void OpenEditDialog(JobItemViewModel? jobVm)
    {
        if (jobVm == null) return;

        _isEditMode = true;
        _editingJob = jobVm.Job;
        DialogJobName = jobVm.Name;
        DialogSourcePath = jobVm.SourcePath;
        DialogTargetPath = jobVm.TargetPath;
        DialogType = jobVm.Type;
        IsDialogOpen = true;
    }

    private void SaveJob()
    {
        if (string.IsNullOrWhiteSpace(DialogJobName))
        {
            MessageBox.Show(_localization.GetString("error_invalid_name"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_pathValidator.IsSourceValid(DialogSourcePath))
        {
            MessageBox.Show(_localization.GetString("error_invalid_source"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_pathValidator.IsTargetValid(DialogTargetPath))
        {
            MessageBox.Show(_localization.GetString("error_invalid_target"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_isEditMode && _editingJob != null)
            {
                // Name uniqueness check excludes the job being edited.
                if (_jobManager.Jobs.Any(j => j != _editingJob && j.Name.Equals(DialogJobName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(_localization.GetString("job_name_alr_exist"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _editingJob.Name = DialogJobName;
                _editingJob.SourcePath = DialogSourcePath;
                _editingJob.TargetPath = DialogTargetPath;
                _editingJob.Type = DialogType;

                MessageBox.Show(_localization.GetString("job_modified"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (_jobManager.Jobs.Any(j => j.Name.Equals(DialogJobName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(_localization.GetString("job_name_alr_exist"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newJob = new SaveJob
                {
                    Name = DialogJobName,
                    SourcePath = DialogSourcePath,
                    TargetPath = DialogTargetPath,
                    Type = DialogType
                };
                _jobManager.AddJob(newJob);

                MessageBox.Show(_localization.GetString("job_created"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            _configManager.SaveJobs(_jobManager);
            RefreshJobs();
            CloseDialog();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        _isEditMode = false;
        _editingJob = null;
    }

    private void ExecuteAll()
    {
        var jobsToExecute = _jobManager.Jobs.ToList();
        ExecuteJobs(jobsToExecute);
    }

    private void ExecuteSelected()
    {
        var selectedJobs = Jobs.Where(j => j.IsSelected).Select(j => j.Job).ToList();
        if (selectedJobs.Count == 0)
        {
            MessageBox.Show(_localization.GetString("select_jobs_to_execute"), _localization.GetString("no_jobs_selected"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ExecuteJobs(selectedJobs);
    }

    // Build the progress popup and start backup execution with the composed pause/stop callbacks.
    private void ExecuteJobs(List<IJob> jobs)
    {
        // Pre-execution check: abort if business software is already running.
        var settings = _configManager.LoadSettings();
        if (_businessChecker.IsBusinessSoftwareRunning(settings.BusinessSoftware))
        {
            ShowBusinessSoftwareDetectedPopup();
            return;
        }

        // Warn if CryptoSoft.exe is missing; user can still proceed without encryption.
        if (!_cryptoRunner.IsCryptoSoftAvailable())
        {
            var result = MessageBox.Show(
                _localization.GetString("cryptosoft_missing"),
                "CryptoSoft",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                return;
        }

        int threadCount = BackupExecutor.MaxConcurrency;
        var jobNames = jobs.Select(j => j.Name).ToList();
        var progressViewModel = new BackupProgressViewModel(_localization, threadCount, jobNames);

        var progressWindow = new BackupProgressWindow(progressViewModel);
        progressWindow.Owner = Application.Current.MainWindow;

        var monitorCts = new CancellationTokenSource();

        // Progress callback: marshal the update onto the UI thread.
        Action<string, double, bool> progressCallback = (jobName, progressPercent, isFailed) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressViewModel.UpdateProgress(jobName, progressPercent, isFailed);
            });
        };

        // Completion callback: enable the OK button and stop the business software monitor.
        Action<bool> completionCallback = (allSuccess) =>
        {
            monitorCts.Cancel();
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressViewModel.SetCompleted();
            });
        };

        // Hard stop: user pressed the emergency stop button for a specific job.
        Func<string, bool> shouldStopFunc = jobName =>
            progressViewModel.IsStopRequested(jobName);

        /* Pause state callback: shows a popup on the first false→true business software transition.
         * A lock-protected flag (pausePopupShown) prevents duplicate popups when multiple
         * jobs all enter the paused state at roughly the same time. */
        bool pausePopupShown = false;
        object pauseLock = new object();
        Action<bool> onPauseStateChanged = (isPaused) =>
        {
            if (isPaused)
            {
                // Only show popup for business software pause, not manual pause.
                bool isBusinessSoftwareRunning = _businessChecker.IsBusinessSoftwareRunning(settings.BusinessSoftware);
                if (!isBusinessSoftwareRunning)
                    return;

                lock (pauseLock)
                {
                    if (pausePopupShown) return;
                    pausePopupShown = true;
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        _localization.GetString("temporary_pause"),
                        _localization.GetString("warning"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }
            else
            {
                lock (pauseLock)
                {
                    pausePopupShown = false;
                }
            }
        };

        /* Soft pause: returns true when business software is running OR when the user has
         * manually paused the specific job. Business software takes priority: while it is
         * running, the Resume button is disabled so the user cannot override it. */
        Func<string, bool> shouldPauseFunc = jobName =>
        {
            bool isBusinessSoftwareRunning = _businessChecker.IsBusinessSoftwareRunning(settings.BusinessSoftware);

            // Reflect business software state in the progress item (UI thread required).
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressViewModel.UpdateBusinessSoftwarePauseState(isBusinessSoftwareRunning);
            });

            return isBusinessSoftwareRunning || progressViewModel.IsManuallyPaused(jobName);
        };

        _backupExecutor.ExecuteWithProgress(
            jobs,
            _logger,
            _stateManager,
            progressCallback,
            completionCallback,
            shouldStopFunc,
            shouldPauseFunc,
            onPauseStateChanged);

        // Background monitor: show the business software popup on each false→true transition.
        if (!string.IsNullOrWhiteSpace(settings.BusinessSoftware))
        {
            bool wasPaused = false;
            Task.Run(async () =>
            {
                while (!monitorCts.Token.IsCancellationRequested)
                {
                    bool isRunning = _businessChecker.IsBusinessSoftwareRunning(settings.BusinessSoftware);
                    if (isRunning && !wasPaused)
                    {
                        wasPaused = true;
                        Application.Current.Dispatcher.Invoke(ShowBusinessSoftwareDetectedPopup);
                    }
                    else if (!isRunning)
                    {
                        wasPaused = false;
                    }
                    try { await Task.Delay(500, monitorCts.Token); }
                    catch (OperationCanceledException) { break; }
                }
            });
        }

        progressWindow.ShowDialog();
    }

    // Shows the "business software detected" warning; reused for the pre-check and mid-run monitor.
    private void ShowBusinessSoftwareDetectedPopup()
    {
        MessageBox.Show(
            _localization.GetString("business_software_detected"),
            "Info",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void DeleteJob(JobItemViewModel? jobVm)
    {
        if (jobVm == null) return;

        var result = MessageBox.Show(
            _localization.GetString("confirm_delete"),
            _localization.GetString("confirm_delete_title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _jobManager.RemoveJob(jobVm.Name);
            _configManager.SaveJobs(_jobManager);
            RefreshJobs();
            MessageBox.Show(_localization.GetString("job_removed"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Update localized strings when language changes.
    public void UpdateLocalizedStrings()
    {
        AddJobText = _localization.GetString("add_job");
        ExecuteAllText = _localization.GetString("execute_all");
        ExecuteSelectedText = _localization.GetString("execute_selected");
        NameHeader = _localization.GetString("name");
        SourceHeader = _localization.GetString("source");
        TargetHeader = _localization.GetString("target");
        TypeHeader = _localization.GetString("type");
        ActionsHeader = _localization.GetString("actions");
        DeleteText = _localization.GetString("delete");
        EditText = _localization.GetString("edit");
        BrowseText = _localization.GetString("browse");
        FullText = _localization.GetString("full");
        DiffText = _localization.GetString("diff");
        CancelText = _localization.GetString("cancel");
        SaveText = _localization.GetString("save");

        foreach (var job in Jobs)
        {
            job.UpdateTypeDisplay();
        }
    }
}
