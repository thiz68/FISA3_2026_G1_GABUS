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
        // Act
        var result = _localizationService.GetString("menu_title");

        // Assert
        Assert.Equal("=== EasySave v1.0 ===", result);
    }

    [Fact]
    public void GetString_WithInvalidKey_ShouldReturnKey()
    {
        // Act
        var result = _localizationService.GetString("invalid_key");

        // Assert
        Assert.Equal("invalid_key", result);
    }

    [Fact]
    public void SetLanguage_ToFrench_ShouldChangeTranslations()
    {
        // Act
        _localizationService.SetLanguage("fr");
        var result = _localizationService.GetString("goodbye");

        // Assert
        Assert.Equal("A bientot!", result);
    }

    [Fact]
    public void SetLanguage_WithInvalidCode_ShouldNotChange()
    {
        // Arrange
        var originalResult = _localizationService.GetString("goodbye");

        // Act
        _localizationService.SetLanguage("invalid");
        var newResult = _localizationService.GetString("goodbye");

        // Assert
        Assert.Equal(originalResult, newResult);
    }

    [Fact]
    public void CurrentLanguage_DefaultsToEnglish()
    {
        // Assert
        Assert.Equal("en", _localizationService.CurrentLanguage);
    }

    [Fact]
    public void LanguageChanged_ShouldFireOnLanguageChange()
    {
        // Arrange
        var eventFired = false;
        _localizationService.LanguageChanged += (s, e) => eventFired = true;

        // Act
        _localizationService.SetLanguage("fr");

        // Assert
        Assert.True(eventFired);
    }
}
