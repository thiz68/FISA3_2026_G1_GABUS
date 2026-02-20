namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;

// Execution of backup jobs
public class BackupExecutor
{
    private readonly FileBackupService _fileBackupService;
    private static ILocalizationService? _localization;

    public BackupExecutor(ILocalizationService localization)
    {
        _fileBackupService = new FileBackupService();
        _localization = localization;
    }

    // Execute jobs in parallel using SemaphoreSlim to limit concurrency based on processor count
    // Returns "backup_completed" if all succeeded, "backup_failed" if any failed
    public string ExecuteSequential(List<IJob> jobs, ILogger logger, IStateManager stateManager, Func<bool>? shouldStop = null)
    {
        // Determine maximum concurrency: clamp between 1 and 8 based on logical processors
        int maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();

        foreach (var job in jobs)
        {
            // Create a task for each job
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // Initialize the state as Active
                    var state = new JobState { State = _localization.GetString("active") };
                    stateManager.UpdateJobState(job, state);

                    // Copy all files from source to target
                    bool success = _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager, _localization, shouldStop);

                    if (success)
                    {
                        // Mark job as completed
                        state.State = _localization.GetString("completed");
                        state.Progression = 100;
                    }
                    else
                    {
                        // Mark job as failed (drive unavailable, USB unplugged, business software detected, etc.)
                        state.State = _localization.GetString("failed");
                    }
                    stateManager.UpdateJobState(job, state);
                    return success;
                }

                finally
                {
                    semaphore.Release();
                }
            }));
        }

        // Wait for all tasks to complete synchronously
        Task.WhenAll(tasks).Wait();

        // Check if all jobs succeeded
        bool allSuccess = tasks.All(t => t.Result);
        return allSuccess ? "backup_completed" : "backup_failed";
    }
}