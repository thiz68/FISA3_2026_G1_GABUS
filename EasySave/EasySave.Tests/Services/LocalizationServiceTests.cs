namespace EasySave.Tests.Services;

using EasySave.Core.Services;
using Xunit;

public class LocalizationServiceTests
{
    private readonly LocalizationService _localizationService;
    
    public LocalizationServiceTests()
    {
        _localizationService = new LocalizationService();
    }
    [Fact]
    
    public void GetString_ExistingKey_ReturnsTranslation()
    {
        // Act
        var result = _localizationService.GetString("menu_title");
        // Assert
        Assert.Equal("=== EasySave v1.0 ===", result);
    }
    [Fact]
    
    public void SetLanguage_ValidLanguage_ChangesLanguage()
    {
        // Act
        _localizationService.SetLanguage("fr");
        var result = _localizationService.GetString("goodbye");
        // Assert
        Assert.Equal("A bientot!", result);
    }
    [Fact]
    
    public void SetLanguage_InvalidLanguage_KeepsCurrentLanguage()
    {
        // Arrange - Default is English
        var originalValue = _localizationService.GetString("goodbye");
        // Act
        _localizationService.SetLanguage("invalid");
        var result = _localizationService.GetString("goodbye");
        // Assert
        Assert.Equal(originalValue, result);
        Assert.Equal("Goodbye!", result);
    }
    [Fact]
    
    public void GetString_AfterSetLanguageFr_ReturnsFrenchText()
    {
        // Arrange
        _localizationService.SetLanguage("fr");
        // Act
        var menuCreate = _localizationService.GetString("menu_create");
        var jobCreated = _localizationService.GetString("job_created");
        // Assert
        Assert.Equal("1. Creer un travail de sauvegarde", menuCreate);
        Assert.Equal("Travail cree avec succes", jobCreated);
    }
    [Theory]
    [InlineData("en", "goodbye", "Goodbye!")]
    [InlineData("fr", "goodbye", "A bientot!")]
    [InlineData("en", "error_max_jobs", "Error: Maximum 5 jobs allowed")]
    [InlineData("fr", "error_max_jobs", "Erreur: Maximum 5 travaux autorises")]
    
    public void GetString_MultipleLanguages_ReturnsCorrectTranslation(string language, string key, string expected)
    {
        // Arrange
        _localizationService.SetLanguage(language);
        // Act
        var result = _localizationService.GetString(key);
        // Assert
        Assert.Equal(expected, result);
    }
}