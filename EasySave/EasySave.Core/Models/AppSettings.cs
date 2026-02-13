namespace EasySave.Core.Models;

// Application settings model for configuration persistence
public class AppSettings
{
    // Current language (en/fr)
    public string Language { get; set; } = "en";

    // Log file format (json/xml) - for future version 1.1
    public string LogFormat { get; set; } = "json";
}
