namespace EasySave.WPF.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Services;
using EasySave.WPF.Commands;

// ViewModel for a single backup job progress item in the progress popup
public class BackupProgressItemViewModel : BaseViewModel
{
    private readonly string _jobName;
    private double _progressPercent;
    private bool _isFailed;
    public bool _stopRequested;

    // Manual pause flag (user clicked Pause button)
    private bool _isManuallyPaused;

    // Flag indicating if paused by business software (priority over manual pause)
    private bool _isBusinessSoftwarePaused;

    // Localized text for Pause and Resume buttons
    private string _pauseText = "Pause";
    private string _resumeText = "Resume";

    // Command for emergency stop button
    public ICommand EmergencyStopCommand { get; }

    // Command for pause/resume button
    public ICommand PauseResumeCommand { get; }

    public string JobName => _jobName;

    // Progress percentage (0-100), or -1 if failed
    public double ProgressPercent
    {
        get => _progressPercent;
        set
        {
            if (SetProperty(ref _progressPercent, value))
            {
                OnPropertyChanged(nameof(ProgressDisplay));
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Flag indicating if the job has failed
    public bool IsFailed
    {
        get => _isFailed;
        set
        {
            if (SetProperty(ref _isFailed, value))
            {
                OnPropertyChanged(nameof(ProgressDisplay));
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Manual pause state (user clicked Pause)
    public bool IsManuallyPaused
    {
        get => _isManuallyPaused;
        set
        {
            if (SetProperty(ref _isManuallyPaused, value))
            {
                OnPropertyChanged(nameof(PauseResumeButtonText));
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Business software pause state (set externally when business software is detected)
    public bool IsBusinessSoftwarePaused
    {
        get => _isBusinessSoftwarePaused;
        set
        {
            if (SetProperty(ref _isBusinessSoftwarePaused, value))
            {
                OnPropertyChanged(nameof(PauseResumeButtonText));
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Localized text for Pause button
    public string PauseText
    {
        get => _pauseText;
        set
        {
            if (SetProperty(ref _pauseText, value))
            {
                OnPropertyChanged(nameof(PauseResumeButtonText));
            }
        }
    }

    // Localized text for Resume button
    public string ResumeText
    {
        get => _resumeText;
        set
        {
            if (SetProperty(ref _resumeText, value))
            {
                OnPropertyChanged(nameof(PauseResumeButtonText));
            }
        }
    }

    // Text displayed on the Pause/Resume button
    // Shows "Resume" if manually paused OR paused by business software, otherwise "Pause"
    public string PauseResumeButtonText
    {
        get
        {
            if (_isManuallyPaused || _isBusinessSoftwarePaused)
                return _resumeText;
            return _pauseText;
        }
    }

    // Display string: "Failed" if failed, otherwise "XX%"
    public string ProgressDisplay
    {
        get
        {
            if (_isFailed)
                return "Failed";
            return $"{Math.Round(_progressPercent, 0)}%";
        }
    }

    public BackupProgressItemViewModel(string jobName)
    {
        _jobName = jobName;
        _progressPercent = 0;
        _isFailed = false;
        _stopRequested = false;
        _isManuallyPaused = false;
        _isBusinessSoftwarePaused = false;
        EmergencyStopCommand = new RelayCommand(ExecuteEmergencyStop, CanEmergencyStop);
        PauseResumeCommand = new RelayCommand(ExecutePauseResume, CanPauseResume);
    }

    private void ExecuteEmergencyStop(object? parameter)
    {
        _stopRequested = true;
        CommandManager.InvalidateRequerySuggested();
    }

    private bool CanEmergencyStop(object? parameter)
    {
        return ProgressPercent < 100 && !IsFailed && !_stopRequested;
    }

    // Toggle pause/resume state
    private void ExecutePauseResume(object? parameter)
    {
        // Toggle manual pause state
        IsManuallyPaused = !IsManuallyPaused;
    }

    // Pause/Resume button is enabled only if:
    // - Job is not completed (progress < 100)
    // - Job has not failed
    // - Job was not stopped by emergency stop
    // - If currently paused by business software, Resume is disabled (greyed out)
    private bool CanPauseResume(object? parameter)
    {
        // Cannot use button if job is completed, failed, or stopped
        if (ProgressPercent >= 100 || IsFailed || _stopRequested)
            return false;

        // If showing "Resume" (job is paused), check if business software is blocking
        if (_isManuallyPaused || _isBusinessSoftwarePaused)
        {
            // Resume is disabled when business software is running
            return !_isBusinessSoftwarePaused;
        }

        // Pause is always available when job is running
        return true;
    }
}

// ViewModel for the backup progress popup window
public class BackupProgressViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;

    // Collection of job progress items for the DataGrid
    public ObservableCollection<BackupProgressItemViewModel> JobProgressItems { get; } = new();

    // Number of available threads for display
    private int _threadCount;
    public int ThreadCount
    {
        get => _threadCount;
        set => SetProperty(ref _threadCount, value);
    }

    // Header text displaying thread count
    private string _headerText = string.Empty;
    public string HeaderText
    {
        get => _headerText;
        set => SetProperty(ref _headerText, value);
    }

    // Flag indicating if all backups are completed (shows OK button)
    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    // Command to close the popup (bound to OK button)
    public ICommand CloseCommand { get; }

    // Action to close the window (set by the Window)
    public Action? CloseAction { get; set; }

    // Localized column headers
    private string _nameColumnHeader = string.Empty;
    public string NameColumnHeader
    {
        get => _nameColumnHeader;
        set => SetProperty(ref _nameColumnHeader, value);
    }

    private string _progressColumnHeader = string.Empty;
    public string ProgressColumnHeader
    {
        get => _progressColumnHeader;
        set => SetProperty(ref _progressColumnHeader, value);
    }

    private string _actionColumnHeader = string.Empty;
    public string ActionColumnHeader
    {
        get => _actionColumnHeader;
        set => SetProperty(ref _actionColumnHeader, value);
    }

    private string _emergencyStopText = string.Empty;
    public string EmergencyStopText
    {
        get => _emergencyStopText;
        set => SetProperty(ref _emergencyStopText, value);
    }

    // Localized text for Pause button
    private string _pauseText = string.Empty;
    public string PauseText
    {
        get => _pauseText;
        set => SetProperty(ref _pauseText, value);
    }

    // Localized text for Resume button
    private string _resumeText = string.Empty;
    public string ResumeText
    {
        get => _resumeText;
        set => SetProperty(ref _resumeText, value);
    }

    public BackupProgressViewModel(ILocalizationService localization, int threadCount, List<string> jobNames)
    {
        _localization = localization;
        _threadCount = threadCount;
        IsCompleted = false;

        // Initialize localized strings
        UpdateLocalizedStrings();

        // Initialize header with thread count
        HeaderText = string.Format(_localization.GetString("backup_progress_header"), threadCount);

        // Create progress items for each job
        foreach (var jobName in jobNames)
        {
            var item = new BackupProgressItemViewModel(jobName);
            // Set localized text for Pause/Resume buttons
            item.PauseText = _pauseText;
            item.ResumeText = _resumeText;
            JobProgressItems.Add(item);
        }

        // Close command for OK button
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke(), _ => IsCompleted);
    }

    // Update progress for a specific job
    public void UpdateProgress(string jobName, double progressPercent, bool isFailed)
    {
        var item = JobProgressItems.FirstOrDefault(x => x.JobName == jobName);
        if (item != null)
        {
            item.ProgressPercent = progressPercent;
            item.IsFailed = isFailed;
        }
    }

    // Update business software pause state for all jobs
    // Called when business software detection state changes
    public void UpdateBusinessSoftwarePauseState(bool isBusinessSoftwareRunning)
    {
        foreach (var item in JobProgressItems)
        {
            item.IsBusinessSoftwarePaused = isBusinessSoftwareRunning;
        }
    }

    // Called when all backups are completed
    public void SetCompleted()
    {
        IsCompleted = true;
        // Force command to re-evaluate CanExecute
        CommandManager.InvalidateRequerySuggested();
    }

    // Update localized strings
    private void UpdateLocalizedStrings()
    {
        NameColumnHeader = _localization.GetString("name");
        ProgressColumnHeader = _localization.GetString("progression");
        ActionColumnHeader = _localization.GetString("actions");
        EmergencyStopText = _localization.GetString("emergency_stop");
        PauseText = _localization.GetString("pause");
        ResumeText = _localization.GetString("resume");
    }

    public bool IsStopRequested(string jobName)
    {
        var item = JobProgressItems.FirstOrDefault(x => x.JobName == jobName);
        return item != null && item._stopRequested;
    }

    // Check if a job is manually paused (not by business software)
    public bool IsManuallyPaused(string jobName)
    {
        var item = JobProgressItems.FirstOrDefault(x => x.JobName == jobName);
        return item != null && item.IsManuallyPaused;
    }
}
