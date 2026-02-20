namespace EasySaveLog;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;


public class Logger : ILogger
{
    private readonly ConfigManager _configManager;
    private ILogWriter _writer;
    private string _logDirectory;

    public Logger(ConfigManager configManager)
    {
        _configManager = configManager;

        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _logDirectory = Path.Combine(appDirectory, "Logs");

        ConfigureWriter();
    }

    private void ConfigureWriter()
    {
        var settings = _configManager.LoadSettings();

        var local = new LocalLogWriter(_logDirectory, () => settings.LogFormat);
        var remote = new RemoteLogWriter(settings.LogServerIp, settings.LogServerPort);

        _writer = settings.LogStorageMode switch
        {
            LogStorageMode.LocalOnly => local,
            LogStorageMode.RemoteOnly => remote,
            LogStorageMode.LocalAndRemote => new CompositeLogWriter(local, remote),
            _ => local
        };
    }

    public async void LogFileTransfer(DateTime timestamp, string jobName,
        string sourceFile, string targetFile,
        long fileSize, long transferTimeMs, long encryptionTimeMs)
    {
        ConfigureWriter();

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

        await _writer.WriteAsync(entry);
    }

    public void Initialize()
    {
        Directory.CreateDirectory(_logDirectory);
    }

    public void SetLogFormat(string format) { }

    public string GetCurrentLogFormat() => _configManager.LoadSettings().LogFormat;

    public void LogBusinessSoftwareStop(DateTime timestamp, string jobName, string businessSoftware)
    {
        LogFileTransfer(timestamp, jobName,
            $"STOPPED: {businessSoftware}", "", 0, -1, 0);
    }
}