/*
 * BackupProgressViewModel: drives the real-time progress popup shown during backup execution.
 * Each job has a BackupProgressItemViewModel that tracks progress, failed/stop state, and
 * two independent pause sources:
 *   - IsManuallyPaused:         user clicked the Pause button.
 *   - IsBusinessSoftwarePaused: set externally when business software is detected.
 * Business software pause takes priority: Resume is disabled (CanPauseResume returns false)
 * while IsBusinessSoftwarePaused is true, so the user cannot override it.
 */
namespace EasySave.WPF.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Core.Interfaces;
using EasySave.Core.Services;
using EasySave.WPF.Commands;

// ViewModel for a single backup job progress item in the progress popup.
public class BackupProgressItemViewModel : BaseViewModel
{
    private readonly string _jobName;
    private double _progressPercent;
    private bool _isFailed;
    public bool _stopRequested;

    // Manual pause flag (user clicked Pause button).
    private bool _isManuallyPaused;

    // Set externally when business software is detected; takes priority over manual pause.
    private bool _isBusinessSoftwarePaused;

    private string _pauseText = "Pause";
    private string _resumeText = "Resume";

    public ICommand EmergencyStopCommand { get; }
    public ICommand PauseResumeCommand { get; }

    public string JobName => _jobName;

    // Progress percentage (0–100), or -1 when the job has failed.
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

    private string _failedText = "Failed";
    public string FailedText
    {
        get => _failedText;
        set
        {
            if (SetProperty(ref _failedText, value))
            {
                OnPropertyChanged(nameof(ProgressDisplay));
            }
        }
    }

    // Shows "Resume" when paused (either source); otherwise shows "Pause".
    public string PauseResumeButtonText
    {
        get
        {
            if (_isManuallyPaused || _isBusinessSoftwarePaused)
                return _resumeText;
            return _pauseText;
        }
    }

    // Shows localized "Failed" if the job failed; otherwise shows the rounded percentage.
    public string ProgressDisplay
    {
        get
        {
            if (_isFailed)
                return _failedText;
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

    private void ExecutePauseResume(object? parameter)
    {
        IsManuallyPaused = !IsManuallyPaused;
    }

    /* Pause/Resume button availability rules:
     *   - Disabled when the job is completed, failed, or stopped.
     *   - When showing "Resume" (job is paused), disabled if business software is still running
     *     because the backup will not resume until the software exits regardless. */
    private bool CanPauseResume(object? parameter)
    {
        if (ProgressPercent >= 100 || IsFailed || _stopRequested)
            return false;

        if (_isManuallyPaused || _isBusinessSoftwarePaused)
        {
            // Resume is blocked while business software holds the pause.
            return !_isBusinessSoftwarePaused;
        }

        return true;
    }
}

// ViewModel for the backup progress popup window.
public class BackupProgressViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;

    public ObservableCollection<BackupProgressItemViewModel> JobProgressItems { get; } = new();

    private int _threadCount;
    public int ThreadCount
    {
        get => _threadCount;
        set => SetProperty(ref _threadCount, value);
    }

    private string _headerText = string.Empty;
    public string HeaderText
    {
        get => _headerText;
        set => SetProperty(ref _headerText, value);
    }

    // When true, all backups have finished and the OK button becomes enabled.
    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public ICommand CloseCommand { get; }

    // Set by the Window so the ViewModel can close it without a direct reference.
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

    private string _pauseText = string.Empty;
    public string PauseText
    {
        get => _pauseText;
        set => SetProperty(ref _pauseText, value);
    }

    private string _resumeText = string.Empty;
    public string ResumeText
    {
        get => _resumeText;
        set => SetProperty(ref _resumeText, value);
    }

    private string _titleText = string.Empty;
    public string TitleText
    {
        get => _titleText;
        set => SetProperty(ref _titleText, value);
    }

    private string _okText = string.Empty;
    public string OkText
    {
        get => _okText;
        set => SetProperty(ref _okText, value);
    }

    private string _failedText = string.Empty;
    public string FailedText
    {
        get => _failedText;
        set => SetProperty(ref _failedText, value);
    }

    public BackupProgressViewModel(ILocalizationService localization, int threadCount, List<string> jobNames)
    {
        _localization = localization;
        _threadCount = threadCount;
        IsCompleted = false;

        UpdateLocalizedStrings();

        HeaderText = string.Format(_localization.GetString("backup_progress_header"), threadCount);

        foreach (var jobName in jobNames)
        {
            var item = new BackupProgressItemViewModel(jobName);
            item.PauseText = _pauseText;
            item.ResumeText = _resumeText;
            item.FailedText = _failedText;
            JobProgressItems.Add(item);
        }

        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke(), _ => IsCompleted);
    }

    public void UpdateProgress(string jobName, double progressPercent, bool isFailed)
    {
        var item = JobProgressItems.FirstOrDefault(x => x.JobName == jobName);
        if (item != null)
        {
            item.ProgressPercent = progressPercent;
            item.IsFailed = isFailed;
        }
    }

    // Propagates the business software detection state to all job items.
    // Called from JobsViewModel's shouldPauseFunc on each polling cycle.
    public void UpdateBusinessSoftwarePauseState(bool isBusinessSoftwareRunning)
    {
        foreach (var item in JobProgressItems)
        {
            item.IsBusinessSoftwarePaused = isBusinessSoftwareRunning;
        }
    }

    public void SetCompleted()
    {
        IsCompleted = true;
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateLocalizedStrings()
    {
        NameColumnHeader = _localization.GetString("name");
        ProgressColumnHeader = _localization.GetString("progression");
        ActionColumnHeader = _localization.GetString("actions");
        EmergencyStopText = _localization.GetString("emergency_stop");
        PauseText = _localization.GetString("pause");
        ResumeText = _localization.GetString("resume");
        TitleText = _localization.GetString("backup_progress_title");
        OkText = _localization.GetString("ok");
        FailedText = _localization.GetString("failed");
    }

    public bool IsStopRequested(string jobName)
    {
        var item = JobProgressItems.FirstOrDefault(x => x.JobName == jobName);
        return item != null && item._stopRequested;
    }

    // Returns true only for manual pause; business software pause is tracked separately.
    public bool IsManuallyPaused(string jobName)
    {
        var item = JobProgressItems.FirstOrDefault(x => x.JobName == jobName);
        return item != null && item.IsManuallyPaused;
    }
}
