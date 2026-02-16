namespace EasySave.Core.Interfaces;

// Interface for logging file transfers
public interface ILogger
{
    // Log a single file transfer with all its details
    void LogFileTransfer(DateTime timestamp, string jobName, string sourceFile, string targetFile, long fileSize, long transferTimeMs, long encryptionTimeMs);

    // Set the log format
    void SetLogFormat(string format);

    // Get log format 
    string GetCurrentLogFormat(); 

    // Initialize the logger (create log file if needed)
    void Initialize();
}
