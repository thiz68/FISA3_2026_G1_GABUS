using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using Xunit;

namespace EasySave.Tests.Services;

public class JobManagerTests
{
    private readonly Mock<ILocalizationService> _mockLocalization;
    private readonly JobManager _jobManager;

    public JobManagerTests()
    {
        _mockLocalization = new Mock<ILocalizationService>();
        _mockLocalization.Setup(l => l.GetString(It.IsAny<string>())).Returns<string>(key => key);
        _jobManager = new JobManager(_mockLocalization.Object);
    }

    [Fact]
    public void AddJob_ShouldAddJobToList()
    {
        // Arrange
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };

        // Act
        _jobManager.AddJob(job);

        // Assert
        Assert.Single(_jobManager.Jobs);
        Assert.Equal("TestJob", _jobManager.Jobs[0].Name);
    }

    [Fact]
    public void AddJob_WhenMaxJobsReached_ShouldThrowException()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _jobManager.AddJob(new SaveJob { Name = $"Job{i}", SourcePath = "/source", TargetPath = "/target", Type = "full" });
        }

        var extraJob = new SaveJob { Name = "ExtraJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _jobManager.AddJob(extraJob));
    }

    [Fact]
    public void AddJob_WithDuplicateName_ShouldThrowException()
    {
        // Arrange
        var job1 = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        var job2 = new SaveJob { Name = "TestJob", SourcePath = "/source2", TargetPath = "/target2", Type = "diff" };

        _jobManager.AddJob(job1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _jobManager.AddJob(job2));
    }

    [Fact]
    public void RemoveJob_ShouldRemoveJobFromList()
    {
        // Arrange
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);

        // Act
        _jobManager.RemoveJob("TestJob");

        // Assert
        Assert.Empty(_jobManager.Jobs);
    }

    [Fact]
    public void GetJob_ByIndex_ShouldReturnCorrectJob()
    {
        // Arrange
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);

        // Act
        var result = _jobManager.GetJob(1);

        // Assert
        Assert.Equal("TestJob", result.Name);
    }

    [Fact]
    public void GetJob_ByName_ShouldReturnCorrectJob()
    {
        // Arrange
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);

        // Act
        var result = _jobManager.GetJob("TestJob");

        // Assert
        Assert.Equal("TestJob", result.Name);
    }

    [Fact]
    public void GetJob_WithInvalidIndex_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _jobManager.GetJob(1));
    }

    [Fact]
    public void MaxJobs_ShouldReturn5()
    {
        // Assert
        Assert.Equal(5, _jobManager.MaxJobs);
    }
}
