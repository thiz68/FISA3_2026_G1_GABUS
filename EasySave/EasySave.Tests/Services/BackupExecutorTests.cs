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
    private readonly JobManager _jobManager;

    public BackupExecutorTests()
    {
        _backupExecutor = new BackupExecutor();
        _mockLogger = new Mock<ILogger>();
        _mockStateManager = new Mock<IStateManager>();
        _jobManager = new JobManager();
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
    public void ExecuteFromCommand_SingleIndex_CallsExecuteSingle()
    {
        // Arrange
        var tempSource = Path.Combine(Path.GetTempPath(), "TestSource_" + Guid.NewGuid());
        var tempTarget = Path.Combine(Path.GetTempPath(), "TestTarget_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSource);

        try
        {
            _jobManager.AddJob(CreateTestJob("Job1", tempSource, tempTarget));

            // Act
            _backupExecutor.ExecuteFromCommand("1", _jobManager, _mockLogger.Object, _mockStateManager.Object);

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
    public void ExecuteFromCommand_RangeFormat_ExecutesMultipleJobs()
    {
        // Arrange
        var tempBase = Path.Combine(Path.GetTempPath(), "TestRange_" + Guid.NewGuid());

        try
        {
            for (int i = 1; i <= 3; i++)
            {
                var source = Path.Combine(tempBase, $"Source{i}");
                var target = Path.Combine(tempBase, $"Target{i}");
                Directory.CreateDirectory(source);
                _jobManager.AddJob(CreateTestJob($"Job{i}", source, target));
            }

            // Act
            _backupExecutor.ExecuteFromCommand("1-3", _jobManager, _mockLogger.Object, _mockStateManager.Object);

            // Assert - Should have called UpdateJobState for each job (at least twice per job: Active + Completed)
            _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.AtLeast(6));
        }
        finally
        {
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
        }
    }

    [Fact]
    public void ExecuteFromCommand_SemicolonFormat_ExecutesSelectedJobs()
    {
        // Arrange
        var tempBase = Path.Combine(Path.GetTempPath(), "TestSemicolon_" + Guid.NewGuid());

        try
        {
            for (int i = 1; i <= 3; i++)
            {
                var source = Path.Combine(tempBase, $"Source{i}");
                var target = Path.Combine(tempBase, $"Target{i}");
                Directory.CreateDirectory(source);
                _jobManager.AddJob(CreateTestJob($"Job{i}", source, target));
            }

            // Act - Execute only jobs 1 and 3
            _backupExecutor.ExecuteFromCommand("1;3", _jobManager, _mockLogger.Object, _mockStateManager.Object);

            // Assert - Should have called UpdateJobState for 2 jobs
            _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.AtLeast(4));
        }
        finally
        {
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
        }
    }

    [Fact]
    public void ExecuteFromCommand_InvalidIndex_DoesNotExecute()
    {
        // Arrange
        var tempSource = Path.Combine(Path.GetTempPath(), "TestInvalid_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSource);

        try
        {
            _jobManager.AddJob(CreateTestJob("Job1", tempSource, tempSource + "_target"));

            // Act - Try to execute job index 10 which doesn't exist
            _backupExecutor.ExecuteFromCommand("10", _jobManager, _mockLogger.Object, _mockStateManager.Object);

            // Assert - Should not have called UpdateJobState
            _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.Never);
        }
        finally
        {
            if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
        }
    }

    [Fact]
    public void ExecuteFromCommand_InvalidFormat_DoesNotExecute()
    {
        // Arrange
        var tempSource = Path.Combine(Path.GetTempPath(), "TestInvalidFormat_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSource);

        try
        {
            _jobManager.AddJob(CreateTestJob("Job1", tempSource, tempSource + "_target"));

            // Act - Invalid format
            _backupExecutor.ExecuteFromCommand("abc", _jobManager, _mockLogger.Object, _mockStateManager.Object);

            // Assert
            _mockStateManager.Verify(s => s.UpdateJobState(It.IsAny<IJob>(), It.IsAny<JobState>()), Times.Never);
        }
        finally
        {
            if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
        }
    }
}
