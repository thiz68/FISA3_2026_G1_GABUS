using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using Xunit;

namespace EasySave.Tests.Services;

public class BackupExecutorTests
{
    private readonly BackupExecutor _backupExecutor;
    private readonly Mock<ILocalizationService> _mockLocalization;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IStateManager> _mockStateManager;

    public BackupExecutorTests()
    {
        _mockLocalization = new Mock<ILocalizationService>();
        _mockLocalization.Setup(l => l.GetString("active")).Returns("Active");
        _mockLocalization.Setup(l => l.GetString("completed")).Returns("Completed");
        _mockLocalization.Setup(l => l.GetString("failed")).Returns("Failed");

        _backupExecutor = new BackupExecutor(_mockLocalization.Object);
        _mockLogger = new Mock<ILogger>();
        _mockStateManager = new Mock<IStateManager>();
    }

    [Fact]
    public void ExecuteSequential_WithEmptyList_ShouldReturnCompleted()
    {
        var jobs = new List<IJob>();
        var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);
        Assert.Equal("backup_completed", result);
    }

    [Fact]
    public void ExecuteSequential_WithInvalidSourcePath_ShouldReturnFailed()
    {
        var job = new SaveJob { Name = "TestJob", SourcePath = "/nonexistent", TargetPath = "/tmp/target", Type = "full" };
        var jobs = new List<IJob> { job };
        var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object);
        Assert.Equal("backup_failed", result);
    }

    [Fact]
    public void ExecuteSequential_WithShouldStopTrue_ShouldStopAndReturnFailed()
    {
        var job = new SaveJob { Name = "TestJob", SourcePath = Directory.GetCurrentDirectory(), TargetPath = "/tmp/target", Type = "full" };
        var jobs = new List<IJob> { job };
        bool shouldStop() => true; // Force stop
        var result = _backupExecutor.ExecuteSequential(jobs, _mockLogger.Object, _mockStateManager.Object, shouldStop);
        Assert.Equal("backup_failed", result);
    }

    [Fact]
    public void MaxConcurrency_ShouldBeClampedBetween1And8()
    {
        Assert.InRange(BackupExecutor.MaxConcurrency, 1, 8);
    }

    // Ajout : Test pour ExecuteWithProgress (asynchrone, mock callbacks)
    [Fact]
    public async Task ExecuteWithProgress_ShouldCallCallbacks()
    {
        var job = new SaveJob { Name = "TestJob", SourcePath = Directory.GetCurrentDirectory(), TargetPath = "/tmp/target", Type = "full" };
        var jobs = new List<IJob> { job };

        var progressCalled = false;
        var completionCalled = false;

        void progressCallback(string name, double progress, bool failed) { progressCalled = true; }
        void completionCallback(bool success) { completionCalled = true; }

        _backupExecutor.ExecuteWithProgress(jobs, _mockLogger.Object, _mockStateManager.Object, progressCallback, completionCallback);
        await Task.Delay(1000); // Attendre fin

        Assert.True(progressCalled);
        Assert.True(completionCalled);
    }
}