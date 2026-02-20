namespace EasySave.Core.Interfaces;

using EasySave.Core.Models;

// Interface for logging file transfers
public interface ILogger : ILogReader
{
    // Log a single file transfer with all its details (synchronous - waits for completion)
    void LogFileTransfer(DateTime timestamp, string jobName, string sourceFile, string targetFile, long fileSize, long transferTimeMs, long encryptionTimeMs);

    // Log a single file transfer with all its details (asynchronous)
    Task LogFileTransferAsync(DateTime timestamp, string jobName, string sourceFile, string targetFile, long fileSize, long transferTimeMs, long encryptionTimeMs);

    // Set the log format
    void SetLogFormat(string format);

    // Get log format
    string GetCurrentLogFormat();

    // Get log storage mode
    LogStorageMode GetLogStorageMode();

    // Initialize the logger (create log file if needed)
    void Initialize();

    // Log when backup is stopped due to business software detection
    void LogBusinessSoftwareStop(DateTime timestamp, string jobName, string businessSoftware);

    // Check if remote server is reachable
    Task<bool> IsRemoteServerReachableAsync();
}