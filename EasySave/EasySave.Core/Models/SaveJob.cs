namespace EasySave.Core.Models;

using EasySave.Core.Enums;
using EasySave.Core.Interfaces;

// Represents a backup job with source, target and type
public class SaveJob : IJob
{
    // Name of the backup job
    public string Name { get; set; } = string.Empty;

    // Source directory to backup
    public string SourcePath { get; set; } = string.Empty;

    // Target directory for backup
    public string TargetPath { get; set; } = string.Empty;

    // Full or Differential backup
    public SaveType Type { get; set; } = SaveType.Full;
}
