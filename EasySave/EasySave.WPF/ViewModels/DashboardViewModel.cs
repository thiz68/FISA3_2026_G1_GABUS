/*
 * DashboardViewModel: displays real-time state and today's log file as text previews.
 * Log reads use a 3 s timeout; if the remote server is slow, the dashboard falls back
 * to reading the local log file directly so the UI never blocks indefinitely.
 * The server-unreachable warning is shown at most once per session (_serverWarningShown flag).
 */
using System.IO;

namespace EasySave.WPF.ViewModels;

using System.Windows;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySaveLog;

public class DashboardViewModel : BaseViewModel
{
    private readonly ILocalizationService _localization;
    private readonly StateManager _stateManager;
    private readonly Logger _logger;
    private readonly ConfigManager _configManager;
    private bool _serverWarningShown = false;

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

    // Server status
    private string _serverStatus = string.Empty;
    public string ServerStatus
    {
        get => _serverStatus;
        set => SetProperty(ref _serverStatus, value);
    }

    private bool _isServerUnreachable = false;
    public bool IsServerUnreachable
    {
        get => _isServerUnreachable;
        set => SetProperty(ref _isServerUnreachable, value);
    }

    public DashboardViewModel(ILocalizationService localization, StateManager stateManager, Logger logger, ConfigManager configManager)
    {
        _localization = localization;
        _stateManager = stateManager;
        _logger = logger;
        _configManager = configManager;

        // Subscribe to remote server failure events raised by CompositeLogWriter.
        Logger.RemoteServerUnreachable += OnRemoteServerUnreachable;

        InitializeDashboard();
    }

    private void OnRemoteServerUnreachable(object? sender, string message)
    {
        IsServerUnreachable = true;
        ServerStatus = _localization.GetString("server_unreachable");

        // Show warning only once per session to avoid repeated popups during a long backup.
        if (!_serverWarningShown)
        {
            _serverWarningShown = true;
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    _localization.GetString("server_unreachable_message"),
                    _localization.GetString("warning"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }
    }

    private async void InitializeDashboard()
    {
        await UpdateLocalizedStringsAsync();
        await RefreshContentAsync();
    }

    // Refresh state and log content, accounting for the configured log format and storage mode.
    public async Task RefreshContentAsync()
    {
        // Read state from the local state file (always available, no network required).
        var stateContent = _stateManager.ReadStateFileContent();
        StateContent = string.IsNullOrEmpty(stateContent)
            ? _localization.GetString("state_preview_placeholder")
            : stateContent;

        var settings = _configManager.LoadSettings();
        var format = _logger.GetCurrentLogFormat();
        var storageMode = settings.LogStorageMode;

        // Check server reachability for remote modes (fire-and-forget, non-blocking).
        if (storageMode != LogStorageMode.LocalOnly)
        {
            _ = CheckServerReachabilityAsync();
        }
        else
        {
            IsServerUnreachable = false;
            ServerStatus = string.Empty;
        }

        // Read logs with a 3 s timeout; fall back to the local file if the remote is slow.
        string logContent = string.Empty;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            var readTask = _logger.ReadCurrentLogAsync();
            var completedTask = await Task.WhenAny(readTask, Task.Delay(3000, cts.Token));

            if (completedTask == readTask)
            {
                logContent = await readTask;
            }
            else
            {
                // Timeout — fall back to local logs so the dashboard stays usable.
                logContent = await ReadLocalLogsAsync(format);

                if (storageMode != LogStorageMode.LocalOnly && !_serverWarningShown)
                {
                    IsServerUnreachable = true;
                    ServerStatus = _localization.GetString("server_unreachable");
                }
            }
        }
        catch
        {
            logContent = await ReadLocalLogsAsync(format);
        }

        string placeholderKey = format == "xml"
            ? "log_preview_placeholder_xml"
            : "log_preview_placeholder_json";

        LogContent = string.IsNullOrEmpty(logContent)
            ? _localization.GetString(placeholderKey)
            : logContent;

        var formatUpper = format.ToUpper();
        LogFileTitle = $"{_localization.GetString("log_file_preview")} ({formatUpper})";
    }

    private async Task<string> ReadLocalLogsAsync(string format)
    {
        try
        {
            var logDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs");

            var extension = format == "xml" ? ".xml" : ".json";
            var path = Path.Combine(logDirectory, DateTime.Now.ToString("yyyy-MM-dd") + extension);

            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
        }
        catch
        {
            // Ignore — dashboard will show placeholder text instead.
        }

        return string.Empty;
    }

    private async Task CheckServerReachabilityAsync()
    {
        try
        {
            var isReachable = await _logger.IsRemoteServerReachableAsync();

            if (!isReachable)
            {
                IsServerUnreachable = true;
                ServerStatus = _localization.GetString("server_unreachable");
            }
            else
            {
                IsServerUnreachable = false;
                ServerStatus = _localization.GetString("server_connected");
            }
        }
        catch
        {
            IsServerUnreachable = true;
            ServerStatus = _localization.GetString("server_unreachable");
        }
    }

    // Re-localize strings and re-read logs with a 3 s timeout on language change.
    public async Task UpdateLocalizedStringsAsync()
    {
        DashboardTitle = _localization.GetString("dashboard");
        StateFileTitle = _localization.GetString("state_file_preview");

        var format = _logger.GetCurrentLogFormat().ToUpper();
        LogFileTitle = $"{_localization.GetString("log_file_preview")} ({format})";

        string logContent = string.Empty;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            var readTask = _logger.ReadCurrentLogAsync();
            var completedTask = await Task.WhenAny(readTask, Task.Delay(3000, cts.Token));

            if (completedTask == readTask)
            {
                logContent = await readTask;
            }
            else
            {
                logContent = await ReadLocalLogsAsync(format.ToLower());
            }
        }
        catch
        {
            logContent = await ReadLocalLogsAsync(format.ToLower());
        }

        string placeholderKey = format.ToLower() == "xml"
            ? "log_preview_placeholder_xml"
            : "log_preview_placeholder_json";

        LogContent = string.IsNullOrEmpty(logContent)
            ? _localization.GetString(placeholderKey)
            : logContent;
    }
}
