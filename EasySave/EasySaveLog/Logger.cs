/*
 * Logger: top-level logging orchestrator for EasySave.
 * Delegates to a writer/reader pair selected by LogStorageMode:
 *   LocalOnly       → LocalLogWriter + LocalLogReader
 *   RemoteOnly      → RemoteLogWriter + RemoteLogReader
 *   LocalAndRemote  → CompositeLogWriter(local, remote) + LocalLogReader
 * A static SemaphoreSlim(1) serializes all async writes to prevent interleaving.
 * Configure() is called before each write so settings changes take effect at runtime
 * without restarting the application.
 */
namespace EasySaveLog;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;

public class Logger : ILogger
{
    private readonly ConfigManager _configManager;
    private ILogWriter _writer = null!;
    private ILogReader _reader = null!;
    private readonly string _logDirectory;

    // Serializes concurrent async writes; static because there is typically one Logger instance.
    private static readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    // Relays CompositeLogWriter.RemoteServerUnreachable to subscribers (e.g., DashboardViewModel).
    public static event EventHandler<string>? RemoteServerUnreachable;

    public Logger(ConfigManager configManager)
    {
        _configManager = configManager;

        _logDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs");

        Configure();

        // Relay the event from CompositeLogWriter up to Logger subscribers.
        CompositeLogWriter.RemoteServerUnreachable += (sender, message) =>
        {
            RemoteServerUnreachable?.Invoke(this, message);
        };
    }

    private void Configure()
    {
        var settings = _configManager.LoadSettings();

        var localWriter = new LocalLogWriter(_logDirectory, () => settings.LogFormat);
        var remoteWriter = new RemoteLogWriter(settings.LogServerIp, settings.LogServerPort);

        var localReader = new LocalLogReader(_logDirectory, () => settings.LogFormat);
        var remoteReader = new RemoteLogReader(settings.LogServerIp, settings.LogServerPort, () => settings.LogFormat);

        switch (settings.LogStorageMode)
        {
            case LogStorageMode.LocalOnly:
                _writer = localWriter;
                _reader = localReader;
                break;

            case LogStorageMode.RemoteOnly:
                _writer = remoteWriter;
                _reader = remoteReader;
                break;

            case LogStorageMode.LocalAndRemote:
                _writer = new CompositeLogWriter(localWriter, remoteWriter);
                _reader = localReader;
                break;
        }
    }

    // Async log write; acquires the semaphore to serialize concurrent calls.
    // Calls Configure() before each write to pick up any settings changes.
    public async Task LogFileTransferAsync(
        DateTime timestamp,
        string jobName,
        string sourceFile,
        string targetFile,
        long fileSize,
        long transferTimeMs,
        long encryptionTimeMs)
    {
        await _writeSemaphore.WaitAsync();

        try
        {
            Configure();

            var settings = _configManager.LoadSettings();
            var format = settings.LogFormat;

            var entry = new LogEntry
            {
                Timestamp = timestamp,
                JobName = jobName,
                SourceFile = sourceFile,
                TargetFile = targetFile,
                FileSize = fileSize,
                TransferTimeMs = transferTimeMs,
                EncryptionTimeMs = encryptionTimeMs
            };

            await _writer.WriteAsync(entry, format);
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    // Synchronous wrapper used by callers that cannot await.
    // Blocks until the log entry is written; errors are swallowed to avoid crashing the backup.
    public void LogFileTransfer(
        DateTime timestamp,
        string jobName,
        string sourceFile,
        string targetFile,
        long fileSize,
        long transferTimeMs,
        long encryptionTimeMs)
    {
        Task.Run(async () =>
        {
            try
            {
                await LogFileTransferAsync(timestamp, jobName, sourceFile, targetFile,
                    fileSize, transferTimeMs, encryptionTimeMs);
            }
            catch
            {
                // Silently ignore — callers relying on this synchronous path must not be interrupted.
            }
        }).Wait(); // Block until write completes so the entry is not lost on process exit.
    }

    public async Task<string> ReadCurrentLogAsync()
    {
        Configure();
        return await _reader.ReadCurrentLogAsync();
    }

    public void Initialize()
    {
        Directory.CreateDirectory(_logDirectory);
    }

    public void SetLogFormat(string format) { }

    public string GetCurrentLogFormat()
        => _configManager.LoadSettings().LogFormat;

    public void LogBusinessSoftwareStop(
        DateTime timestamp,
        string jobName,
        string businessSoftware)
    {
        LogFileTransfer(
            timestamp,
            jobName,
            $"STOPPED: {businessSoftware}",
            string.Empty,
            0,
            -1,
            0);
    }

    // Returns the configured log storage mode (used by DashboardViewModel for reachability checks).
    public LogStorageMode GetLogStorageMode()
        => _configManager.LoadSettings().LogStorageMode;

    // Checks whether the remote log server is currently reachable.
    // Returns true immediately for LocalOnly mode (remote is irrelevant).
    public async Task<bool> IsRemoteServerReachableAsync()
    {
        var settings = _configManager.LoadSettings();
        if (settings.LogStorageMode == LogStorageMode.LocalOnly)
            return true;

        var remoteWriter = new RemoteLogWriter(settings.LogServerIp, settings.LogServerPort);
        return await remoteWriter.IsServerReachableAsync();
    }
}
