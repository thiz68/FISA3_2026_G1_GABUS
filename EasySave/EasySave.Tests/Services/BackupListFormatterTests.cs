using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using Xunit;

namespace EasySave.Tests.Services;

public class BackupListFormatterTests
{
    private readonly BackupListFormatter _formatter;
    private readonly Mock<IJobManager> _mockJobManager;
    private static ILocalizationService? _localization;


    public BackupListFormatterTests(ILocalizationService localization)
    {
        _localization = localization;
        _formatter = new BackupListFormatter(_localization);
        _mockJobManager = new Mock<IJobManager>();

        // Setup mock jobs
        var jobs = new List<IJob>
        {
            new SaveJob { Name = "Job1", SourcePath = "/src1", TargetPath = "/tgt1", Type = "full" },
            new SaveJob { Name = "Job2", SourcePath = "/src2", TargetPath = "/tgt2", Type = "diff" },
            new SaveJob { Name = "Job3", SourcePath = "/src3", TargetPath = "/tgt3", Type = "full" }
        };

        _mockJobManager.Setup(m => m.Jobs).Returns(jobs.AsReadOnly());
        _mockJobManager.Setup(m => m.MaxJobs).Returns(5);
        _mockJobManager.Setup(m => m.GetJob(It.IsAny<int>())).Returns<int>(i => jobs[i - 1]);
    }

    [Fact]
    public void FormatJobList_WithSingleIndex_ShouldReturnCorrectJob()
    {
        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1", _mockJobManager.Object);

        // Assert
        Assert.True(success);
        Assert.Single(jobs);
        Assert.Equal("Job1", jobs[0].Name);
    }

    [Fact]
    public void FormatJobList_WithRange_ShouldReturnCorrectJobs()
    {
        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1-3", _mockJobManager.Object);

        // Assert
        Assert.True(success);
        Assert.Equal(3, jobs.Count);
    }

    [Fact]
    public void FormatJobList_WithSemicolonSeparated_ShouldReturnCorrectJobs()
    {
        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1;3", _mockJobManager.Object);

        // Assert
        Assert.True(success);
        Assert.Equal(2, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job3", jobs[1].Name);
    }

    [Fact]
    public void FormatJobList_WithInvalidIndex_ShouldReturnFalse()
    {
        // Act
        var (success, message, jobs) = _formatter.FormatJobList("10", _mockJobManager.Object);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void FormatJobList_WithInvalidFormat_ShouldReturnFalse()
    {
        // Act
        var (success, message, jobs) = _formatter.FormatJobList("abc", _mockJobManager.Object);

        // Assert
        Assert.False(success);
    }
}
