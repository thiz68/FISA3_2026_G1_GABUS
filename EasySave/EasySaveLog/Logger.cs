namespace EasySaveLog;

using System.Text.Json;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

//This class handles logging for the backyp application
public class Logger : ILogger
{
    private readonly string _logDirectory;

    //Constructor
    public Logger()
    {
        //Get the application's directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        //Build the Path with "\":
        _logDirectory = Path.Combine(appDirectory, "Logs");
    }

    //Creates log folder, once app start
    public void Initialize()
    {
        //CreateDirectory does nothing if the folder already exists
        Directory.CreateDirectory(_logDirectory);
    }

    //Information for the file transfer
    //   - timestamp: wheb the transfer happened
    //   - jobName: name of the backup job
    //   - sourceFile: original file path
    //   - targetFile: destination file path
    //   - fileSize: size of the file in bytes
    //   - transferTimeMs: how long the transfer took in milliseconds
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

        //Load log entries from the file to a list
        var entries = new List<LogEntry>();

        // Try to read existing log file, handle errors if file/drive is unavailable
        try
        {
            if (File.Exists(logFilePath))
            {
                //Read JSON
                var existingJson = File.ReadAllText(logFilePath);

                //Convertion to list
                var existingEntries = JsonSerializer.Deserialize<List<LogEntry>>(existingJson);

                if (existingEntries != null)
                {
                    entries = existingEntries;
                }
            }
        }
        catch (IOException)
        {
            // Could not read log file, start with empty list
        }

        entries.Add(entry);

        //Convertion list back to JSON
        var options = new JsonSerializerOptions { WriteIndented = true };

        var json = JsonSerializer.Serialize(entries, options);

        // Try to write log file, handle errors if drive becomes unavailable
        try
        {
            File.WriteAllText(logFilePath, json);
        }
        catch (IOException)
        {
            // Could not write log file, skip logging this entry
        }
    }

    //Method returns the path of log file
    private string GetDailyLogFilePath()
    {
        var fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        return Path.Combine(_logDirectory, fileName);
    }

    // Read the current day's log file content (for dashboard display)
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
