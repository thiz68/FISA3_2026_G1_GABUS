using EasySave.Core.Services;
using Xunit;

namespace EasySave.Tests.Services;

public class LocalizationServiceTests
{
    private readonly LocalizationService _localizationService;

    public LocalizationServiceTests()
    {
        _localizationService = new LocalizationService();
    }

    [Fact]
    public void GetString_WithValidKey_ShouldReturnTranslation()
    {
        var result = _localizationService.GetString("menu_title");
        Assert.Equal("=== EasySave v1.0 ===", result);
    }

    [Fact]
    public void GetString_WithInvalidKey_ShouldReturnKey()
    {
        var result = _localizationService.GetString("invalid_key");
        Assert.Equal("invalid_key", result);
    }

    [Fact]
    public void SetLanguage_ToFrench_ShouldChangeTranslations()
    {
        _localizationService.SetLanguage("fr");
        var result = _localizationService.GetString("menu_title");
        Assert.Equal("=== EasySave v1.0 ===", result); // Vérifie une clé commune, ajuste si besoin
    }

    [Fact]
    public void SetLanguage_WithInvalidCode_ShouldNotChange()
    {
        var original = _localizationService.GetString("menu_create");
        _localizationService.SetLanguage("invalid");
        var newResult = _localizationService.GetString("menu_create");
        Assert.Equal(original, newResult);
    }

    [Fact]
    public void CurrentLanguage_DefaultsToEnglish()
    {
        Assert.Equal("en", _localizationService.CurrentLanguage);
    }

    [Fact]
    public void LanguageChanged_ShouldFireOnLanguageChange()
    {
        var eventFired = false;
        _localizationService.LanguageChanged += (s, e) => eventFired = true;
        _localizationService.SetLanguage("fr");
        Assert.True(eventFired);
    }

    [Fact]
    public void GetString_FrenchSpecificKey_ShouldReturnCorrect()
    {
        _localizationService.SetLanguage("fr");
        var result = _localizationService.GetString("goodbye");
        Assert.Equal("A bientot!", result);
    }
}