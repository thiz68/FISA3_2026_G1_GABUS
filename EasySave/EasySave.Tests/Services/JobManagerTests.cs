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
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);
        Assert.Single(_jobManager.Jobs);
        Assert.Equal("TestJob", _jobManager.Jobs[0].Name);
    }

    [Fact]
    public void AddJob_WithDuplicateName_ShouldThrowException()
    {
        var job1 = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        var job2 = new SaveJob { Name = "TestJob", SourcePath = "/source2", TargetPath = "/target2", Type = "diff" };
        _jobManager.AddJob(job1);
        var ex = Assert.Throws<InvalidOperationException>(() => _jobManager.AddJob(job2));
        Assert.Equal("job_name_alr_exist", ex.Message);
    }

    [Fact]
    public void RemoveJob_ShouldRemoveJobFromList()
    {
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);
        _jobManager.RemoveJob("TestJob");
        Assert.Empty(_jobManager.Jobs);
    }

    [Fact]
    public void RemoveJob_NonExistent_ShouldDoNothing()
    {
        _jobManager.RemoveJob("NonExistent");
        Assert.Empty(_jobManager.Jobs);
    }

    [Fact]
    public void GetJob_ByIndex_ShouldReturnCorrectJob()
    {
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);
        var result = _jobManager.GetJob(1);
        Assert.Equal("TestJob", result.Name);
    }

    [Fact]
    public void GetJob_ByName_ShouldReturnCorrectJob()
    {
        var job = new SaveJob { Name = "TestJob", SourcePath = "/source", TargetPath = "/target", Type = "full" };
        _jobManager.AddJob(job);
        var result = _jobManager.GetJob("TestJob");
        Assert.Equal("TestJob", result.Name);
    }

    [Fact]
    public void GetJob_WithInvalidIndex_ShouldThrowException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _jobManager.GetJob(1));
    }

    [Fact]
    public void GetJob_WithInvalidName_ShouldThrowException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _jobManager.GetJob("Invalid"));
        Assert.Equal("error_not_found", ex.Message);
    }

    [Fact]
    public void MaxJobs_ShouldReturn5()
    {
        Assert.Equal(5, _jobManager.MaxJobs);
    }
}