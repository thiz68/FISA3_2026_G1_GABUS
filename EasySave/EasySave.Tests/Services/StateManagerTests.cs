using EasySave.Core.Models;
using EasySave.Core.Services;
using Moq;
using System.IO;
using Xunit;

namespace EasySave.Tests.Services;

public class StateManagerTests
{
    private readonly StateManager _stateManager;

    public StateManagerTests()
    {
        _stateManager = new StateManager();
    }

    [Fact]
    public void UpdateJobState_ShouldSaveToFile()
    {
        var job = new SaveJob { Name = "TestJob" };
        var state = new JobState { State = "Active" };
        _stateManager.UpdateJobState(job, state);

        var content = _stateManager.ReadStateFileContent();
        Assert.Contains("TestJob", content);
    }

    [Fact]
    public void ReadStateFileContent_NonExistent_ShouldReturnEmpty()
    {
        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "states.json"));
        Assert.Empty(_stateManager.ReadStateFileContent());
    }
}