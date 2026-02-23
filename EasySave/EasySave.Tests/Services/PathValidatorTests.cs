using EasySave.Core.Services;
using System.IO;
using Xunit;

namespace EasySave.Tests.Services;

public class PathValidatorTests
{
    private readonly PathValidator _validator;

    public PathValidatorTests()
    {
        _validator = new PathValidator();
    }

    [Fact]
    public void IsSourceValid_ExistingNonExePath_ShouldReturnTrue()
    {
        var tempPath = Path.GetTempPath();
        Assert.True(_validator.IsSourceValid(tempPath));
    }

    [Fact]
    public void IsSourceValid_NonExistentPath_ShouldReturnFalse()
    {
        Assert.False(_validator.IsSourceValid("/nonexistent"));
    }

    [Fact]
    public void IsTargetValid_AbsoluteWritablePath_ShouldReturnTrue()
    {
        var tempPath = Path.GetTempPath();
        Assert.True(_validator.IsTargetValid(tempPath));
    }

    [Fact]
    public void IsTargetValid_RelativePath_ShouldReturnFalse()
    {
        Assert.False(_validator.IsTargetValid("relative/path"));
    }
}