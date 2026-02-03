namespace EasySave.Core.Interfaces;

// Interface for managing backup jobs (max 5 in v1.0)
public interface IJobManager
{
    // List of all backup jobs (read-only)
    IReadOnlyList<IJob> Jobs { get; }

    // Add a new backup job
    void AddJob(IJob job);

    // Remove a job by its name
    void RemoveJob(string name);

    // Get a job by its index (0-4)
    IJob GetJob(int index);

    // Get a job by its name
    IJob GetJob(string name);
}
