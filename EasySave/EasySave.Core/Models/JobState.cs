namespace EasySave.Core.Models;

public class JobState
{
    // Backup jobs name
    public string Name { get; set; } = string.Empty;

    // Source path
    public string JobSourcePath { get; set; } = string.Empty;

    // Target path
    public string JobTargetPath { get; set; } = string.Empty;

    // Timestamp to get the time of last update
    public DateTime Timestamp { get; set; }

    // State of the job exec
    public string State { get; set; } = "Inactive";

    // Number of files to save
    public int TotalFilesToCopy { get; set; }

    // Size of files to save in bytes
    public long TotalFilesSize { get; set; }

    // Number of files remaining to copy
    public int NbFilesLeftToDo { get; set; }

    // Size remaining to copy in bytes
    public long NbSizeLeftToDo { get; set; }

    // Progession of job in %
    public double Progression { get; set; }

    // Current source file being saved
    public string CurrentSourceFilePath { get; set; } = string.Empty;

    // Current target file being copied
    public string CurrentTargetFilePath { get; set; } = string.Empty;
}
