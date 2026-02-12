namespace EasySave.Tests.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using Xunit;

public class BackupListFormatterTests
{
    private readonly BackupListFormatter _formatter;
    private readonly Mock<ILocalizationService> _mockLocalization;
    private readonly JobManager _jobManager;

    public BackupListFormatterTests()
    {
        _formatter = new BackupListFormatter();
        _mockLocalization = new Mock<ILocalizationService>();
        _jobManager = new JobManager(_mockLocalization.Object);
    }

    private SaveJob CreateTestJob(string name)
    {
        return new SaveJob
        {
            Name = name,
            SourcePath = "/source/path",
            TargetPath = "/target/path",
            Type = "full"
        };
    }

    private void AddTestJobs(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            _jobManager.AddJob(CreateTestJob($"Job{i}"));
        }
    }

    [Fact]
    public void FormatJobList_SingleIndex_ReturnsOneJob()
    {
        // Arrange
        AddTestJobs(3);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Empty(message);
        Assert.Single(jobs);
        Assert.Equal("Job1", jobs[0].Name);
    }

    [Fact]
    public void FormatJobList_RangeFormat_ReturnsMultipleJobs()
    {
        // Arrange
        AddTestJobs(5);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1-3", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Empty(message);
        Assert.Equal(3, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job2", jobs[1].Name);
        Assert.Equal("Job3", jobs[2].Name);
    }

    [Fact]
    public void FormatJobList_ReversedRange_ReturnsJobsInOrder()
    {
        // Arrange
        AddTestJobs(5);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("3-1", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Equal(3, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job2", jobs[1].Name);
        Assert.Equal("Job3", jobs[2].Name);
    }

    [Fact]
    public void FormatJobList_SemicolonSeparated_ReturnsSpecificJobs()
    {
        // Arrange
        AddTestJobs(5);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1;3;5", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Empty(message);
        Assert.Equal(3, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job3", jobs[1].Name);
        Assert.Equal("Job5", jobs[2].Name);
    }

    [Fact]
    public void FormatJobList_MixedFormat_ReturnsAllSpecifiedJobs()
    {
        // Arrange
        AddTestJobs(5);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1-2;5", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Equal(3, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal("Job2", jobs[1].Name);
        Assert.Equal("Job5", jobs[2].Name);
    }

    [Fact]
    public void FormatJobList_DuplicateIndexes_ReturnsUniqueJobs()
    {
        // Arrange
        AddTestJobs(3);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1;1;2;2", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Equal(2, jobs.Count);
    }

    [Fact]
    public void FormatJobList_InvalidIndex_ReturnsFailure()
    {
        // Arrange
        AddTestJobs(3);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("10", _jobManager);

        // Assert
        Assert.False(success);
        Assert.Empty(jobs);
    }

    [Fact]
    public void FormatJobList_InvalidFormat_ReturnsFailure()
    {
        // Arrange
        AddTestJobs(3);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("abc", _jobManager);

        // Assert
        Assert.False(success);
        Assert.Empty(jobs);
    }

    [Fact]
    public void FormatJobList_ZeroIndex_ReturnsFailure()
    {
        // Arrange
        AddTestJobs(3);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("0", _jobManager);

        // Assert
        Assert.False(success);
        Assert.Empty(jobs);
    }

    [Fact]
    public void FormatJobList_NegativeInRange_ParsesAsValidRange()
    {
        // Arrange
        AddTestJobs(3);

        // Act - "-1-2" with RemoveEmptyEntries becomes range 1-2
        var (success, message, jobs) = _formatter.FormatJobList("-1-2", _jobManager);

        // Assert - Code interprets this as valid range 1-2
        Assert.True(success);
        Assert.Equal(2, jobs.Count);
    }

    [Fact]
    public void FormatJobList_EmptyJobList_ReturnsFailure()
    {
        // Arrange - no jobs added

        // Act
        var (success, message, jobs) = _formatter.FormatJobList("1", _jobManager);

        // Assert
        Assert.False(success);
        Assert.Empty(jobs);
    }

    [Fact]
    public void FormatJobList_WhitespaceHandling_ReturnsCorrectJobs()
    {
        // Arrange
        AddTestJobs(3);

        // Act
        var (success, message, jobs) = _formatter.FormatJobList(" 1 ; 2 ", _jobManager);

        // Assert
        Assert.True(success);
        Assert.Equal(2, jobs.Count);
    }
}
