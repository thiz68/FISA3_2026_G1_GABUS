namespace EasySave.Core.Models;

// Application settings model for configuration persistence
public class AppSettings
{
    // Current language (en/fr)
    public string Language { get; set; } = "en";

    // Log file format (json/xml) - for future version 1.1
    public string LogFormat { get; set; } = "json";
    
    public string ExtensionsToEncrypt { get; set; } = string.Empty;

    public string BusinessSoftware { get; set; } = string.Empty;

    // Single priority extension (e.g. ".exe"). Non-priority files are blocked until all
    // priority files across all running jobs are fully processed.
    public string PriorityExtension { get; set; } = string.Empty;
}
