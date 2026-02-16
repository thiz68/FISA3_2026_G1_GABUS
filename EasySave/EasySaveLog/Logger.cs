namespace EasySaveLog;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using System.Text.Json;
using System.Xml.Serialization;

// This class handles logging for the backup application
public class Logger : ILogger
{
    private readonly ConfigManager _configManager;
    private readonly string _logDirectory;
    private string _logFormat = "json"; // Default format

    // Constructor
    public Logger(ConfigManager configManager)
    {
        _configManager = configManager;
        // Get the application's base directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        // Build Logs folder path
        _logDirectory = Path.Combine(appDirectory, "Logs");
        RefreshFormat();
    }

    // Get log format
    public string GetCurrentLogFormat() => _logFormat;

    // Sets the desired log format ("json" or "xml")
    public void SetLogFormat(string format)
    {
        _logFormat = format.ToLower() == "xml" ? "xml" : "json";
    }

    // Creates log folder if it doesn't exist (called once at startup)
    public void Initialize()
    {
        // CreateDirectory is idempotent
        Directory.CreateDirectory(_logDirectory);
    }

    private void RefreshFormat()
    {
        _logFormat = _configManager.LoadSettings().LogFormat.ToLower();
    }

    // Logs file transfer information
    // - timestamp: when the transfer happened
    // - jobName: name of the backup job
    // - sourceFile: original file path
    // - targetFile: destination file path
    // - fileSize: size of the file in bytes
    // - transferTimeMs: how long the transfer took in milliseconds
    // - encryptionTimeMs: how long the encryption was going
    public void LogFileTransfer(DateTime timestamp, string jobName, string sourceFile, string targetFile, long fileSize,
        long transferTimeMs, long encryptionTimeMs = 0)
    {

        RefreshFormat();

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

        //Path to log file
        var logFilePath = GetDailyLogFilePath();

        // Load existing entries or start with empty list
        var entries = LoadExistingEntries(logFilePath);

        entries.Add(entry);

        // Save updated entries in the selected format
        SaveEntries(logFilePath, entries);
    }

    // Logs when backup is stopped due to business software detection
    public void LogBusinessSoftwareStop(DateTime timestamp, string jobName, string businessSoftware)
    {
        RefreshFormat();

        var entry = new LogEntry
        {
            Timestamp = timestamp,
            JobName = jobName,
            SourceFile = $"STOPPED: Business software detected ({businessSoftware})",
            TargetFile = string.Empty,
            FileSize = 0,
            TransferTimeMs = -1
        };

        var logFilePath = GetDailyLogFilePath();
        var entries = LoadExistingEntries(logFilePath);
        entries.Add(entry);
        SaveEntries(logFilePath, entries);
    }

    // Loads existing log entries depending on current format
    private List<LogEntry> LoadExistingEntries(string path)
    {
        if (!File.Exists(path)) return new List<LogEntry>();

        try
        {
            var content = File.ReadAllText(path);

            if (_logFormat == "xml")
            {
                var serializer = new XmlSerializer(typeof(List<LogEntry>));
                using var reader = new StringReader(content);
                var result = serializer.Deserialize(reader) as List<LogEntry>;
                return result ?? new List<LogEntry>();
            }
            else // json
            {
                return JsonSerializer.Deserialize<List<LogEntry>>(content) ?? new List<LogEntry>();
            }
        }
        catch (Exception)
        {
            // Return empty list on any read/deserialization error
            return new List<LogEntry>();
        }
    }

    // Saves the list of entries in the current format (JSON or XML)
    private void SaveEntries(string path, List<LogEntry> entries)
    {
        try
        {
            string content;

            if (_logFormat == "xml")
            {
                var serializer = new XmlSerializer(typeof(List<LogEntry>));
                using var writer = new StringWriter();
                serializer.Serialize(writer, entries);
                content = writer.ToString();
            }
            else // json
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                content = JsonSerializer.Serialize(entries, options);
            }

            File.WriteAllText(path, content);
        }
        catch (IOException)
        {
        }
    }

    // Returns the path of the current log file
    private string GetDailyLogFilePath()
    {
        var extension = _logFormat == "xml" ? ".xml" : ".json";
        var fileName = DateTime.Now.ToString("yyyy-MM-dd") + extension;
        return Path.Combine(_logDirectory, fileName);
    }

    // Reads the current log file content (for dashbaord)
    public string ReadLogFileContent()
    {
 
        RefreshFormat();

        try
        {
            var logFilePath = GetDailyLogFilePath();
            if (File.Exists(logFilePath))
            {
                return File.ReadAllText(logFilePath);
            }
        }
        catch (IOException)
        {
        }
        return string.Empty;
    }
}