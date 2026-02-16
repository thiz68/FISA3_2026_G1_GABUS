namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;

// Execution of backup jobs
public class BackupExecutor
{
    private readonly FileBackupService _fileBackupService;
    private static ILocalizationService _localization = null!;
    private CancellationTokenSource? _cancellationTokenSource;

    public BackupExecutor()
    {
        _fileBackupService = new FileBackupService();
    }

    // Request to stop the current backup execution
    public void RequestStop()
    {
        _cancellationTokenSource?.Cancel();
    }

    // Check if backup is currently running
    public bool IsRunning => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;

    // Execute jobs sequentially
    // Returns "backup_completed" if all succeeded, "backup_failed" if any failed, "backup_stopped" if stopped by user
    public string ExecuteSequential(List<IJob> jobs, ILogger logger, IStateManager stateManager)
    {
        _localization = new LocalizationService();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        bool allSuccess = true;

        foreach (var job in jobs)
        {
            // Check if stop was requested before starting next job
            if (token.IsCancellationRequested)
            {
                logger.LogJobStopped(DateTime.Now, job.Name, "stop by application");
                var stoppedState = new JobState { State = _localization.GetString("stopped") };
                stateManager.UpdateJobState(job, stoppedState);
                _cancellationTokenSource = null;
                return "backup_stopped";
            }

            // Initialize the state as Active
            var state = new JobState { State = _localization.GetString("active") };
            stateManager.UpdateJobState(job, state);

            // Copy all files from source to target
            var (success, wasStopped) = _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager, _localization, token);

            if (wasStopped)
            {
                // Log the stop and update state
                logger.LogJobStopped(DateTime.Now, job.Name, "stop by application");
                state.State = _localization.GetString("stopped");
                stateManager.UpdateJobState(job, state);
                _cancellationTokenSource = null;
                return "backup_stopped";
            }
            else if (success)
            {
                // Mark job as completed
                state.State = _localization.GetString("completed");
                state.Progression = 100;
            }
            else
            {
                // Mark job as failed (drive unavailable, USB unplugged, etc.)
                state.State = _localization.GetString("failed");
                Console.WriteLine($"{job.Name}: {_localization.GetString("backup_failed")}");
                allSuccess = false;
            }

            stateManager.UpdateJobState(job, state);
        }

        _cancellationTokenSource = null;
        return allSuccess ? "backup_completed" : "backup_failed";
    }

    // Execute jobs sequentially with provided localization service (for WPF)
    public string ExecuteSequential(List<IJob> jobs, ILogger logger, IStateManager stateManager, ILocalizationService localization)
    {
        _localization = localization;
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        bool allSuccess = true;

        foreach (var job in jobs)
        {
            // Check if stop was requested before starting next job
            if (token.IsCancellationRequested)
            {
                logger.LogJobStopped(DateTime.Now, job.Name, "stop by application");
                var stoppedState = new JobState { State = _localization.GetString("stopped") };
                stateManager.UpdateJobState(job, stoppedState);
                _cancellationTokenSource = null;
                return "backup_stopped";
            }

            // Initialize the state as Active
            var state = new JobState { State = _localization.GetString("active") };
            stateManager.UpdateJobState(job, state);

            // Copy all files from source to target
            var (success, wasStopped) = _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager, _localization, token);

            if (wasStopped)
            {
                // Log the stop and update state
                logger.LogJobStopped(DateTime.Now, job.Name, "stop by application");
                state.State = _localization.GetString("stopped");
                stateManager.UpdateJobState(job, state);
                _cancellationTokenSource = null;
                return "backup_stopped";
            }
            else if (success)
            {
                // Mark job as completed
                state.State = _localization.GetString("completed");
                state.Progression = 100;
            }
            else
            {
                // Mark job as failed (drive unavailable, USB unplugged, etc.)
                state.State = _localization.GetString("failed");
                allSuccess = false;
            }

            stateManager.UpdateJobState(job, state);
        }

        _cancellationTokenSource = null;
        return allSuccess ? "backup_completed" : "backup_failed";
    }
}
