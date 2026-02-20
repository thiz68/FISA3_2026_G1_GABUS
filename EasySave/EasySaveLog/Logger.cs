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
    private static readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    /// <summary>
    /// Event raised when remote server is unreachable
    /// </summary>
    public static event EventHandler<string>? RemoteServerUnreachable;

    public Logger(ConfigManager configManager)
    {
        _configManager = configManager;

        _logDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs");

        Configure();

        // Subscribe to CompositeLogWriter events
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

    /// <summary>
    /// Log a file transfer - returns Task to allow awaiting completion
    /// </summary>
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

    /// <summary>
    /// Legacy synchronous method - fires and forgets but ensures completion
    /// </summary>
    public void LogFileTransfer(
        DateTime timestamp,
        string jobName,
        string sourceFile,
        string targetFile,
        long fileSize,
        long transferTimeMs,
        long encryptionTimeMs)
    {
        // Use Task.Run to avoid blocking, but ensure the write completes
        Task.Run(async () =>
        {
            try
            {
                await LogFileTransferAsync(timestamp, jobName, sourceFile, targetFile,
                    fileSize, transferTimeMs, encryptionTimeMs);
            }
            catch
            {
                // Silently ignore errors in legacy method
            }
        }).Wait(); // Wait for completion to ensure log is written
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

    /// <summary>
    /// Get current log storage mode
    /// </summary>
    public LogStorageMode GetLogStorageMode()
        => _configManager.LoadSettings().LogStorageMode;

    /// <summary>
    /// Check if remote server is configured and reachable
    /// </summary>
    public async Task<bool> IsRemoteServerReachableAsync()
    {
        var settings = _configManager.LoadSettings();
        if (settings.LogStorageMode == LogStorageMode.LocalOnly)
            return true; // Not using remote, so "reachable" is N/A

        var remoteWriter = new RemoteLogWriter(settings.LogServerIp, settings.LogServerPort);
        return await remoteWriter.IsServerReachableAsync();
    }
}