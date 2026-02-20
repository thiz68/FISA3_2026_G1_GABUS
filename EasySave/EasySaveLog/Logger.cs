namespace EasySaveLog;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;

public class Logger : ILogger
{
    private readonly ConfigManager _configManager;
    private ILogWriter _writer;
    private ILogReader _reader;
    private readonly string _logDirectory;

    public Logger(ConfigManager configManager)
    {
        _configManager = configManager;

        _logDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs");

        Configure();
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

    public async void LogFileTransfer(
        DateTime timestamp,
        string jobName,
        string sourceFile,
        string targetFile,
        long fileSize,
        long transferTimeMs,
        long encryptionTimeMs)
    {
        Configure();

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

        try
        {
            await _writer.WriteAsync(entry);
        }
        catch
        {
        }
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
}