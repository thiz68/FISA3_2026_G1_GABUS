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

    // Priority file extension(s). Supports a single value (".exe") or a semicolon-separated list (".exe;.pdf;.zip").
    // Extensions must be dot-prefixed and lowercase. Order matters: first extension is processed before second, etc.
    // Empty or whitespace = priority disabled.
    public string PriorityExtension { get; set; } = string.Empty;

    // Max size (KB) above which at most one transfer is allowed at a time. 0 = disabled.
    public long LargeFileThresholdKB { get; set; } = 0;

    //LOG DOCKER
    public LogStorageMode LogStorageMode { get; set; } = LogStorageMode.LocalOnly;

    public string LogServerIp { get; set; } = "127.0.0.1";

    public int LogServerPort { get; set; } = 5000;
}
