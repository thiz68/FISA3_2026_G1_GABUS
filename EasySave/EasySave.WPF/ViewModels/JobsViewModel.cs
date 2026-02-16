namespace EasySave.WPF.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.WPF.Commands;
using EasySaveLog;

// ViewModel for a single job item in the list
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

    // For selection in DataGrid
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

// ViewModel for the Jobs view
// Manages the list of backup jobs and operations
public class JobsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly IJobManager _jobManager;
    private readonly ConfigManager _configManager;
    private readonly BackupExecutor _backupExecutor;
    private readonly Logger _logger;
    private readonly StateManager _stateManager;
    private readonly PathValidator _pathValidator;

    // Observable collection of jobs for DataGrid binding
    public ObservableCollection<JobItemViewModel> Jobs { get; } = new();

    // Selected job for editing/deletion
    private JobItemViewModel? _selectedJob;
    public JobItemViewModel? SelectedJob
    {
        get => _selectedJob;
        set => SetProperty(ref _selectedJob, value);
    }

    // Commands
    public ICommand AddJobCommand { get; }
    public ICommand ExecuteAllCommand { get; }
    public ICommand ExecuteSelectedCommand { get; }
    public ICommand DeleteJobCommand { get; }
    public ICommand EditJobCommand { get; }
    public ICommand StopBackupCommand { get; }

    // Backup running state
    private bool _isBackupRunning;
    public bool IsBackupRunning
    {
        get => _isBackupRunning;
        set => SetProperty(ref _isBackupRunning, value);
    }

    // Stop notification popup
    private bool _stopNotificationVisible;
    public bool StopNotificationVisible
    {
        get => _stopNotificationVisible;
        set => SetProperty(ref _stopNotificationVisible, value);
    }

    private string _stopNotificationText = string.Empty;
    public string StopNotificationText
    {
        get => _stopNotificationText;
        set => SetProperty(ref _stopNotificationText, value);
    }

    private string _stopJobText = string.Empty;
    public string StopJobText
    {
        get => _stopJobText;
        set => SetProperty(ref _stopJobText, value);
    }

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
        BackupExecutor backupExecutor, Logger logger, StateManager stateManager, PathValidator pathValidator)
    {
        _localization = localization;
        _jobManager = jobManager;
        _configManager = configManager;
        _backupExecutor = backupExecutor;
        _logger = logger;
        _stateManager = stateManager;
        _pathValidator = pathValidator;

        // Initialize commands
        AddJobCommand = new RelayCommand(_ => OpenAddDialog(), _ => !IsBackupRunning);
        ExecuteAllCommand = new RelayCommand(_ => ExecuteAll(), _ => Jobs.Count > 0 && !IsBackupRunning);
        ExecuteSelectedCommand = new RelayCommand(_ => ExecuteSelected(), _ => Jobs.Any(j => j.IsSelected) && !IsBackupRunning);
        DeleteJobCommand = new RelayCommand(param => DeleteJob(param as JobItemViewModel), _ => !IsBackupRunning);
        EditJobCommand = new RelayCommand(param => OpenEditDialog(param as JobItemViewModel), _ => !IsBackupRunning);
        SaveJobCommand = new RelayCommand(_ => SaveJob());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        StopBackupCommand = new RelayCommand(_ => StopBackup(), _ => IsBackupRunning);

        UpdateLocalizedStrings();
        RefreshJobs();
    }

    // Refresh the jobs list from the manager
    public void RefreshJobs()
    {
        Jobs.Clear();
        foreach (var job in _jobManager.Jobs)
        {
            Jobs.Add(new JobItemViewModel(job, _localization));
        }
    }

    // Open dialog to add a new job
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

    // Open dialog to edit an existing job
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

    // Save job (add or edit)
    private void SaveJob()
    {
        // Validate inputs
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
                // Check if name is unique (excluding current job)
                if (_jobManager.Jobs.Any(j => j != _editingJob && j.Name.Equals(DialogJobName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(_localization.GetString("job_name_alr_exist"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update existing job
                _editingJob.Name = DialogJobName;
                _editingJob.SourcePath = DialogSourcePath;
                _editingJob.TargetPath = DialogTargetPath;
                _editingJob.Type = DialogType;

                MessageBox.Show(_localization.GetString("job_modified"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Check if name is unique
                if (_jobManager.Jobs.Any(j => j.Name.Equals(DialogJobName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(_localization.GetString("job_name_alr_exist"), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Add new job
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

    // Close the dialog
    private void CloseDialog()
    {
        IsDialogOpen = false;
        _isEditMode = false;
        _editingJob = null;
    }

    // Execute all jobs sequentially
    private void ExecuteAll()
    {
        var jobsToExecute = _jobManager.Jobs.ToList();
        ExecuteJobs(jobsToExecute);
    }

    // Execute selected jobs
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

    // Execute a list of jobs asynchronously
    private async void ExecuteJobs(List<IJob> jobs)
    {
        IsBackupRunning = true;
        MessageBox.Show(_localization.GetString("backup_started"), "Info", MessageBoxButton.OK, MessageBoxImage.Information);

        // Run backup in background thread to keep UI responsive
        var result = await Task.Run(() => _backupExecutor.ExecuteSequential(jobs, _logger, _stateManager, _localization));

        IsBackupRunning = false;

        // Show stop notification if backup was stopped
        if (result == "backup_stopped")
        {
            ShowStopNotification();
        }

        MessageBox.Show(_localization.GetString(result), "Result", MessageBoxButton.OK,
            result == "backup_completed" ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    // Stop the current backup execution
    private void StopBackup()
    {
        _backupExecutor.RequestStop();
    }

    // Show the stop notification popup for 3 seconds
    private async void ShowStopNotification()
    {
        StopNotificationText = _localization.GetString("job_stopped_notification");
        StopNotificationVisible = true;
        await Task.Delay(3000);
        StopNotificationVisible = false;
    }

    // Delete a job
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

    // Update localized strings when language changes
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
        StopJobText = _localization.GetString("stop_job");

        // Update job type displays
        foreach (var job in Jobs)
        {
            job.UpdateTypeDisplay();
        }
    }
}
