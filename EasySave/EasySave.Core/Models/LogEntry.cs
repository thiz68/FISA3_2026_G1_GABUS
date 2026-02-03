namespace EasySave.Core.Models;

// Represents a single log entry for a file transfer
public class LogEntry
{
    // When the transfer happened
    public DateTime Timestamp { get; set; }

    // Name of the backup job
    public string JobName { get; set; } = string.Empty;

    // Full path of the source file
    public string SourceFile { get; set; } = string.Empty;

    // Full path of the target file
    public string TargetFile { get; set; } = string.Empty;

    // Size of the file in bytes
    public long FileSize { get; set; }

    // Time taken to transfer in milliseconds
    public long TransferTimeMs { get; set; }
}
