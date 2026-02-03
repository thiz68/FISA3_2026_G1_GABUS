namespace EasySave.Core.Models;

public class LogEntry
{
    // Timestamp to get the time of the transfer
    public DateTime Timestamp { get; set; }

    // Name of the job
    public string JobName { get; set; } = string.Empty;

    // Source file path
    public string SourceFile { get; set; } = string.Empty;

    // Target file path
    public string TargetFile { get; set; } = string.Empty;

    // Size of file in bytes
    public long FileSize { get; set; }

    // Time taken to transfer in milliseconds
    public long TransferTimeMs { get; set; }
}
