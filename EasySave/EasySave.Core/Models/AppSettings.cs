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

    //LOG DOCKER
    public LogStorageMode LogStorageMode { get; set; } = LogStorageMode.LocalOnly;

    public string LogServerIp { get; set; } = "127.0.0.1";

    public int LogServerPort { get; set; } = 5000;
}
