namespace EasySave.Core.Models;

// Represents the current state of a backup job (for state.json)
public class JobState
{
    // Name of the backup job
    public string Name { get; set; } = string.Empty;

    // Source directory path
    public string JobSourcePath { get; set; } = string.Empty;

    // Target directory path
    public string JobTargetPath { get; set; } = string.Empty;

    // When the state was last updated
    public DateTime Timestamp { get; set; }

    // Current state: Active, Inactive, etc.
    public string State { get; set; } = "Inactive";

    // Total number of files to copy
    public int TotalFilesToCopy { get; set; }

    // Total size of all files in bytes
    public long TotalFilesSize { get; set; }

    // Number of files remaining to copy
    public int NbFilesLeftToDo { get; set; }

    // Size remaining to copy in bytes
    public long NbSizeLeftToDo { get; set; }

    // Progress percentage (0 to 100)
    public double Progression { get; set; }

    // Current file being copied (source path)
    public string CurrentSourceFilePath { get; set; } = string.Empty;

    // Current file being copied (target path)
    public string CurrentTargetFilePath { get; set; } = string.Empty;
}
