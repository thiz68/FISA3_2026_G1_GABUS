namespace EasySaveLog;

using System.Text.Json;
using System.Xml.Serialization;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

// This class handles logging for the backup application
public class Logger : ILogger
{
    private readonly string _logDirectory;
    private string _logFormat = "json"; // Default format

    // Constructor
    public Logger()
    {
        // Get the application's base directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        // Build Logs folder path
        _logDirectory = Path.Combine(appDirectory, "Logs");
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

    // Logs file transfer information
    // - timestamp: when the transfer happened
    // - jobName: name of the backup job
    // - sourceFile: original file path
    // - targetFile: destination file path
    // - fileSize: size of the file in bytes
    // - transferTimeMs: how long the transfer took in milliseconds
    public void LogFileTransfer(DateTime timestamp, string jobName, string sourceFile, string targetFile, long fileSize,
        long transferTimeMs)
    {
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            JobName = jobName,
            SourceFile = sourceFile,
            TargetFile = targetFile,
            FileSize = fileSize,
            TransferTimeMs = transferTimeMs,
        };

        //Path to log file
        var logFilePath = GetDailyLogFilePath();

        // Load existing entries or start with empty list
        var entries = LoadExistingEntries(logFilePath);

        entries.Add(entry);

        // Save updated entries in the selected format
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
            // Ignore read errors
        }
        return string.Empty;
    }
}