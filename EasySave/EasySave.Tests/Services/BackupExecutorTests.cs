using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using Xunit;

namespace EasySave.Tests.Services;

public class BackupExecutorTests
{
    private readonly BackupExecutor _backupExecutor;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IStateManager> _mockStateManager;

    public BackupExecutorTests()
    {
        _backupExecutor = new BackupExecutor();
        _mockLogger = new Mock<ILogger>();
        _mockStateManager = new Mock<IStateManager>();
    }

    [Fact]
    public void ExecuteSequential_WithEmptyList_ShouldReturnCompleted()
    {
        // Arrange
        var jobs = new List<IJob>();

        // Act
        var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);

        // Assert
        Assert.Equal("backup_completed", result);
    }

    [Fact]
    public void ExecuteSequential_WithInvalidSourcePath_ShouldReturnFailed()
    {
        // Arrange
        var job = new SaveJob
        {
            Name = "TestJob",
            SourcePath = "/nonexistent/path",
            TargetPath = "/tmp/target",
            Type = "full"
        };
        var jobs = new List<IJob> { job };

        // Act
        var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);

        // Assert
        Assert.Equal("backup_failed", result);
    }
}
