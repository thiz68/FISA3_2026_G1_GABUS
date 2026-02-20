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
            JobProgressItems.Add(new BackupProgressItemViewModel(jobName));
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
    }
}
