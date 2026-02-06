using EasySave.Core.Models;

namespace EasySave.Core.Interfaces;

// Interface for managing real-time backup state
public interface IStateManager
{
    // Update the state of a specific job
    void UpdateJobState(IJob job, JobState state);

    // Save the current state to file
    void SaveState();
}
