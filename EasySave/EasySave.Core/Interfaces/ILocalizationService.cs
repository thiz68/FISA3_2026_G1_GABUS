namespace EasySave.Core.Interfaces;

// Interface for multi-language support (FR/EN)
public interface ILocalizationService
{
    // Get a translated string by its key
    string GetString(string key);

    // Change the current language
    void SetLanguage(string languageCode);
}
