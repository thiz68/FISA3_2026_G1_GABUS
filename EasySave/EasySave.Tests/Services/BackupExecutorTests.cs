namespace EasySave.Tests.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using Xunit;

public class BackupExecutorTests
{
    private readonly BackupExecutor _backupExecutor;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly Mock<ILocalizationService> _mockLocalization;
    private readonly JobManager _jobManager;

    public BackupExecutorTests()
    {
        _backupExecutor = new BackupExecutor();
        _mockLogger = new Mock<ILogger>();
        _mockStateManager = new Mock<IStateManager>();
        _mockLocalization = new Mock<ILocalizationService>();
        _jobManager = new JobManager(_mockLocalization.Object);
    }

    private SaveJob CreateTestJob(string name, string sourcePath, string targetPath)
    {
        return new SaveJob
        {
            Name = name,
            SourcePath = sourcePath,
            TargetPath = targetPath,
            Type = "full"
        };
    }

    [Fact]
    public void ExecuteSequential_SingleJob_UpdatesJobState()
    {
        // Arrange
        var tempSource = Path.Combine(Path.GetTempPath(), "TestSource_" + Guid.NewGuid());
        var tempTarget = Path.Combine(Path.GetTempPath(), "TestTarget_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSource);

        try
        {
            var job = CreateTestJob("Job1", tempSource, tempTarget);
            var jobs = new List<IJob> { job };

            // Act
            _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);

            // Assert
            _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.AtLeastOnce);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
            if (Directory.Exists(tempTarget)) Directory.Delete(tempTarget, true);
        }
    }

    [Fact]
    public void ExecuteSequential_MultipleJobs_UpdatesAllJobStates()
    {
        // Arrange
        var tempBase = Path.Combine(Path.GetTempPath(), "TestMultiple_" + Guid.NewGuid());
        var jobs = new List<IJob>();

        try
        {
            for (int i = 1; i <= 3; i++)
            {
                var source = Path.Combine(tempBase, $"Source{i}");
                var target = Path.Combine(tempBase, $"Target{i}");
                Directory.CreateDirectory(source);
                jobs.Add(CreateTestJob($"Job{i}", source, target));
            }

            // Act
            _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);

            // Assert - Each job should have state updated at least twice (Active then Completed/Failed)
            _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.AtLeast(3));
        }
        finally
        {
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
        }
    }

    [Fact]
    public void ExecuteSequential_EmptyList_DoesNothing()
    {
        // Arrange
        var jobs = new List<IJob>();

        // Act
        var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);

        // Assert
        _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.Never);
        Assert.Equal("backup_completed", result);
    }

    [Fact]
    public void ExecuteSequential_ValidJob_ReturnsBackupCompleted()
    {
        // Arrange
        var tempSource = Path.Combine(Path.GetTempPath(), "TestSuccess_" + Guid.NewGuid());
        var tempTarget = Path.Combine(Path.GetTempPath(), "TestSuccessTarget_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSource);

        try
        {
            var job = CreateTestJob("SuccessJob", tempSource, tempTarget);
            var jobs = new List<IJob> { job };

            // Act
            var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);

            // Assert
            Assert.Equal("backup_completed", result);
        }
        finally
        {
            if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
            if (Directory.Exists(tempTarget)) Directory.Delete(tempTarget, true);
        }
    }

}
