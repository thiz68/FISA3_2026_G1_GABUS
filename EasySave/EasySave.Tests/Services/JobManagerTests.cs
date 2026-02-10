namespace EasySave.Tests.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Xunit;

public class JobManagerTests
{
    private readonly JobManager _jobManager;
    
    public JobManagerTests()
    {
        var localization = new LocalizationService();
        _jobManager = new JobManager(localization);
    }
    
    private static SaveJob CreateTestJob(string name = "TestJob")
    {
        return new SaveJob
        {
            Name = name,
            SourcePath = "/source/path",
            TargetPath = "/target/path",
            Type = "full"
        };
    }
    [Fact]
    
    public void AddJob_ValidJob_JobAddedToList()
    {
        // Arrange
        var job = CreateTestJob();
        // Act
        _jobManager.AddJob(job);
        // Assert
        Assert.Single(_jobManager.Jobs);
        Assert.Equal("TestJob", _jobManager.Jobs[0].Name);
    }
    [Fact]
    
    public void AddJob_MaxJobsReached_ThrowsException()
    {
        // Arrange - Add 5 jobs (max allowed)
        for (int i = 1; i <= 5; i++)
        {
            _jobManager.AddJob(CreateTestJob($"Job{i}"));
        }
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        _jobManager.AddJob(CreateTestJob("Job6")));
    }
    [Fact]
    
    public void AddJob_DuplicateName_ThrowsException()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob("DuplicateJob"));
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        _jobManager.AddJob(CreateTestJob("DuplicateJob")));
    }
    [Fact]
    
    public void RemoveJob_ExistingJob_JobRemoved()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob("ToRemove"));
        Assert.Single(_jobManager.Jobs);
        // Act
        _jobManager.RemoveJob("ToRemove");
        // Assert
        Assert.Empty(_jobManager.Jobs);
    }
    [Fact]
    
    public void RemoveJob_NonExistingJob_NoException()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob("ExistingJob"));
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _jobManager.RemoveJob("NonExisting"));
        Assert.Null(exception);
        Assert.Single(_jobManager.Jobs);
    }
    [Fact]
    
    public void GetJob_ValidIndex_ReturnsJob()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob("FirstJob"));
        _jobManager.AddJob(CreateTestJob("SecondJob"));
        // Act
        var job = _jobManager.GetJob(2);
        // Assert
        Assert.Equal("SecondJob", job.Name);
    }
    [Fact]
    
    public void GetJob_ValidName_ReturnsJob()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob("NamedJob"));
        // Act
        var job = _jobManager.GetJob("NamedJob");
        // Assert
        Assert.Equal("NamedJob", job.Name);
    }
    [Fact]
    
    public void GetJob_InvalidIndex_ThrowsException()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob());
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _jobManager.GetJob(5));
    }
    [Fact]
    
    public void GetJob_InvalidName_ThrowsException()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob());
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _jobManager.GetJob("NonExisting"));
    }
    [Fact]
    
    public void Jobs_ReturnsReadOnlyList()
    {
        // Arrange
        _jobManager.AddJob(CreateTestJob("Job1"));
        _jobManager.AddJob(CreateTestJob("Job2"));
        // Act
        var jobs = _jobManager.Jobs;
        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<IJob>>(jobs);
        Assert.Equal(2, jobs.Count);
    }
}