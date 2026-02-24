namespace EasySave.WPF.ViewModels;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Threading;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.WPF.Commands;
using EasySaveLog;

// Display model for a single log entry row in the Activity view
public class LogItemViewModel
{
    public string Timestamp { get; set; } = string.Empty;
    public string Level     { get; set; } = "INFO";
    public string Message   { get; set; } = string.Empty;

    public string LevelColor => Level switch
    {
        "ERROR" => "#CC3333",
        "WARN"  => "#CC7700",
        _       => "#0078D4",
    };

    public string LevelBackground => Level switch
    {
        "ERROR" => "#20FF3333",
        "WARN"  => "#20FF9900",
        _       => "#200078D4",
    };
}

// ViewModel for the Activity panel — reads and displays the current daily log file
public class ActivityViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly Logger               _logger;
    private readonly DispatcherTimer      _timer;
    private readonly string               _logDirectory;

    // Raised after each refresh so the view can scroll to the newest entry
    public event EventHandler? ScrollToBottomRequested;

    public ObservableCollection<LogItemViewModel> LogEntries { get; } = new();

    private string _activityTitle = string.Empty;
    public string ActivityTitle
    {
        get => _activityTitle;
        set => SetProperty(ref _activityTitle, value);
    }

    private string _openLogsFolderText = string.Empty;
    public string OpenLogsFolderText
    {
        get => _openLogsFolderText;
        set => SetProperty(ref _openLogsFolderText, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _isEmpty = true;
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public ICommand OpenLogsFolderCommand { get; }
    public ICommand RefreshCommand        { get; }

    public ActivityViewModel(ILocalizationService localization, Logger logger)
    {
        _localization = localization;
        _logger       = logger;
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        OpenLogsFolderCommand = new RelayCommand(_ => OpenLogsFolder());
        RefreshCommand        = new RelayCommand(_ => _ = LoadLogsAsync());

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await LoadLogsAsync();
        _timer.Start();

        UpdateLocalizedStrings();
        _ = LoadLogsAsync();
    }

    public void UpdateLocalizedStrings()
    {
        ActivityTitle      = _localization.GetString("activity");
        OpenLogsFolderText = _localization.GetString("open_logs_folder");
    }

    private void OpenLogsFolder()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            Process.Start(new ProcessStartInfo("explorer.exe", _logDirectory)
            {
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    public async Task LoadLogsAsync()
    {
        try
        {
            var format    = _logger.GetCurrentLogFormat();
            var extension = format == "xml" ? ".xml" : ".json";
            var path      = Path.Combine(_logDirectory,
                                         DateTime.Now.ToString("yyyy-MM-dd") + extension);

            if (!File.Exists(path))
            {
                StatusText = _localization.GetString("no_logs_today");
                IsEmpty    = true;
                return;
            }

            string rawContent;
            try
            {
                rawContent = await File.ReadAllTextAsync(path);
            }
            catch
            {
                return; // file locked — skip this cycle
            }

            var entries = ParseLogEntries(rawContent, format);
            var last50  = entries.TakeLast(50).ToList();

            LogEntries.Clear();
            foreach (var item in last50)
                LogEntries.Add(item);

            IsEmpty    = LogEntries.Count == 0;
            StatusText = LogEntries.Count > 0
                ? $"{LogEntries.Count} entries"
                : _localization.GetString("no_logs_today");

            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }
        catch { /* silently ignore all errors */ }
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static List<LogItemViewModel> ParseLogEntries(string content, string format)
    {
        try
        {
            return format == "xml" ? ParseXmlEntries(content) : ParseJsonEntries(content);
        }
        catch
        {
            return new List<LogItemViewModel>();
        }
    }

    private static List<LogItemViewModel> ParseJsonEntries(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var entries = JsonSerializer.Deserialize<List<LogEntry>>(json, options);
        return entries?.Select(CreateItem).ToList() ?? new List<LogItemViewModel>();
    }

    private static List<LogItemViewModel> ParseXmlEntries(string xml)
    {
        var result = new List<LogItemViewModel>();
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            foreach (var el in doc.Descendants("LogEntry"))
            {
                var entry = new LogEntry
                {
                    Timestamp       = DateTime.TryParse(el.Element("Timestamp")?.Value,       out var ts)   ? ts   : DateTime.MinValue,
                    JobName         = el.Element("JobName")?.Value         ?? string.Empty,
                    SourceFile      = el.Element("SourceFile")?.Value      ?? string.Empty,
                    TargetFile      = el.Element("TargetFile")?.Value      ?? string.Empty,
                    FileSize        = long.TryParse(el.Element("FileSize")?.Value,        out var fs)   ? fs   : 0,
                    TransferTimeMs  = long.TryParse(el.Element("TransferTimeMs")?.Value,  out var ttms) ? ttms : 0,
                    EncryptionTimeMs= long.TryParse(el.Element("EncryptionTimeMs")?.Value,out var etms) ? etms : 0,
                };
                result.Add(CreateItem(entry));
            }
        }
        catch { /* malformed XML — return empty */ }
        return result;
    }

    private static LogItemViewModel CreateItem(LogEntry entry)
    {
        var rawText = $"{entry.JobName} {entry.SourceFile} {entry.TargetFile}";

        string level;
        if (entry.TransferTimeMs < 0 ||
            rawText.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            level = "WARN";
        }
        else if (rawText.Contains("ERROR",        StringComparison.OrdinalIgnoreCase) ||
                 rawText.Contains("FAILED",       StringComparison.OrdinalIgnoreCase) ||
                 rawText.Contains("Exception",    StringComparison.OrdinalIgnoreCase) ||
                 rawText.Contains("Access denied",StringComparison.OrdinalIgnoreCase))
        {
            level = "ERROR";
        }
        else
        {
            level = "INFO";
        }

        var sourceFileName = Path.GetFileName(entry.SourceFile);
        if (string.IsNullOrEmpty(sourceFileName))
            sourceFileName = entry.SourceFile;

        var message = string.IsNullOrEmpty(entry.JobName) ? "(no job)" : entry.JobName;

        if (!string.IsNullOrEmpty(sourceFileName))
            message += $"  ·  {sourceFileName}";

        if (entry.FileSize > 0)
            message += $"  ·  {FormatBytes(entry.FileSize)}";

        message += entry.TransferTimeMs >= 0
            ? $"  ·  {entry.TransferTimeMs}ms"
            : $"  ·  stopped";

        return new LogItemViewModel
        {
            Timestamp = entry.Timestamp != DateTime.MinValue
                ? entry.Timestamp.ToString("HH:mm:ss")
                : "--:--:--",
            Level   = level,
            Message = message,
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024)     return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }
}
