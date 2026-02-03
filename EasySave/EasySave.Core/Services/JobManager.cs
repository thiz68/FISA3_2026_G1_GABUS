namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;

// Store backup jobs and methods
public class JobManager : IJobManager
{
    // List that contains all saved jobs
    private readonly List<IJob> _jobs = new();

    // Maximum number of jobs in the list
    private const int MaxJobs = 5;

    // Return a the list of jobs in read only
    public IReadOnlyList<IJob> Jobs => _jobs.AsReadOnly();

    // Add job to the list fucntion
    public void AddJob(IJob job)
    {
        // Check if list full
        if (_jobs.Count >= MaxJobs)
            throw new InvalidOperationException("Maximum 5 jobs allowed");

        // Check if job with the same name already exist
        if (_jobs.Any(j => j.Name == job.Name))
            throw new InvalidOperationException("Job name already exists");

        // -> Add the job
        _jobs.Add(job);
    }

    // Remove a job from the list function
    public void RemoveJob(string name)
    {
        // Search for a job with the same name
        var job = _jobs.FirstOrDefault(j => j.Name == name);
        
        // Delete job if it exist
        if (job != null)
            _jobs.Remove(job);
    }

    // Get job object with index
    public IJob GetJob(int index)
    {
        return _jobs[index - 1];
    }

    // Get job object with name
    public IJob GetJob(string name)
    {
        return _jobs.First(j => j.Name == name);
    }
}
