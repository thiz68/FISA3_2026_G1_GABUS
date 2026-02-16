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

    // Execute jobs sequentially
    // Returns "backup_completed" if all succeeded, "backup_failed" if any failed

    public string ExecuteSequential(List<IJob> jobs, ILogger logger, IStateManager stateManager, Func<bool>? shouldStop = null)
    {
        bool allSuccess = true;

        foreach (var job in jobs)
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
                // Mark job as failed (drive unavailable, USB unplugged, etc.)
                state.State = _localization.GetString("failed");
                allSuccess = false;

                // If stopped due to business software, log it
                if (shouldStop?.Invoke() == true)
                {
                    var settings = new ConfigManager().LoadSettings();
                    logger.LogBusinessSoftwareStop(DateTime.Now, job.Name, settings.BusinessSoftware);
                }
            }

            stateManager.UpdateJobState(job, state);
        }

        return allSuccess ? "backup_completed" : "backup_failed";
    }
}
