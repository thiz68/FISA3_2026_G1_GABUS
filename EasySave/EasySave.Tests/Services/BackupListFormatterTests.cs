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
    private readonly Mock<ILocalizationService> _mockLocalization;

    public BackupListFormatterTests()
    {
        _mockLocalization = new Mock<ILocalizationService>();
        _mockLocalization.Setup(l => l.GetString("invalid_choice")).Returns("Invalid choice");
        _mockLocalization.Setup(l => l.GetString("error_not_found")).Returns("Job not found");

        _formatter = new BackupListFormatter(_mockLocalization.Object);
        _mockJobManager = new Mock<IJobManager>();

        // Setup mock jobs
        var jobs = new List<IJob>
        {
            new SaveJob { Name = "Job1", SourcePath = "/src1", TargetPath = "/tgt1", Type = "full" },
            new SaveJob { Name = "Job2", SourcePath = "/src2", TargetPath = "/tgt2", Type = "diff" },
            new SaveJob { Name = "Job3", SourcePath = "/src3", TargetPath = "/tgt3", Type = "full" }
        };

        _mockJobManager.Setup(m => m.Jobs).Returns(jobs.AsReadOnly());
        _mockJobManager.Setup(m => m.MaxJobs).Returns(5); // Même si commenté dans le code, on mock pour compatibilité
        _mockJobManager.Setup(m => m.GetJob(It.IsAny<int>())).Returns<int>(i => jobs[i - 1]);
    }

    [Fact]
    public void FormatJobList_WithSingleIndex_ShouldReturnCorrectJob()
    {
        var (success, message, jobs) = _formatter.FormatJobList("1", _mockJobManager.Object);
        Assert.True(success);
        Assert.Empty(message);
        Assert.Single(jobs);
        Assert.Equal("Job1", jobs[0].Name);
    }

    [Fact]
    public void FormatJobList_WithRange_ShouldReturnCorrectJobs()
    {
        var (success, message, jobs) = _formatter.FormatJobList("1-3", _mockJobManager.Object);
        Assert.True(success);
        Assert.Empty(message);
        Assert.Equal(3, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job3", jobs[2].Name);
    }

    [Fact]
    public void FormatJobList_WithSemicolonSeparated_ShouldReturnCorrectJobs()
    {
        var (success, message, jobs) = _formatter.FormatJobList("1;3", _mockJobManager.Object);
        Assert.True(success);
        Assert.Empty(message);
        Assert.Equal(2, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job3", jobs[1].Name);
    }

    [Fact]
    public void FormatJobList_WithInvalidIndex_ShouldReturnFalse()
    {
        var (success, message, jobs) = _formatter.FormatJobList("10", _mockJobManager.Object);
        Assert.False(success);
        Assert.Equal("Job not found", message);
        Assert.Empty(jobs);
    }

    [Fact]
    public void FormatJobList_WithDuplicateIndexes_ShouldReturnUniqueJobs()
    {
        var (success, message, jobs) = _formatter.FormatJobList("1;1", _mockJobManager.Object);
        Assert.True(success);
        Assert.Single(jobs);
    }
}