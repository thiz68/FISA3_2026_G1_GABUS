namespace EasySave.Core.Interfaces;

// Interface for a backup job configuration
public interface IJob
{
    // Name of the backup job
    string Name { get; set; }

    // Source directory path
    string SourcePath { get; set; }

    // Target directory path
    string TargetPath { get; set; }

    // Type of backup (Full or Differential)
    string Type { get; set; }
}
