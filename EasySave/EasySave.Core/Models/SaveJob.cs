namespace EasySave.Core.Models;

using EasySave.Core.Interfaces;

// Job object
public class SaveJob : IJob
{
    // Name of the job
    public string Name { get; set; } = string.Empty;

    // Source path
    public string SourcePath { get; set; } = string.Empty;

    // Target path
    public string TargetPath { get; set; } = string.Empty;

    // Type of backup (full/differentila)
    public string Type { get; set; } = "full";
}
