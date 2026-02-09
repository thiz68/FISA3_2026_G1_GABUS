namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;

//Execution of backup jobs
public class BackupExecutor
{
    private readonly FileBackupService _fileBackupService;
    private static ILocalizationService _localization = null!;

    public BackupExecutor()
    {
        _fileBackupService = new FileBackupService();
    }

    //Single Job
    //Returns true if backup succeeded, false if it failed
    public bool ExecuteSingle(IJob job, ILogger logger, IStateManager stateManager)
    {
        _localization = new LocalizationService();

        //Initialize the state as Active
        var state = new JobState { State = _localization.GetString("active") };
        stateManager.UpdateJobState(job, state);

        //Copy all files from source to target
        bool success = _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager);

        if (success)
        {
            //Mark job as completed
            state.State = _localization.GetString("completed");
            state.Progression = 100;
        }
        else
        {
            //Mark job as failed (drive unavailable, USB unplugged, etc.)
            state.State = _localization.GetString("failed");
            Console.WriteLine($"{job.Name}: {_localization.GetString("backup_failed")}");
        }

        stateManager.UpdateJobState(job, state);
        return success;
    }

    //Multiple jobs
    //Returns true if all backups succeeded, false if any failed
    public bool ExecuteSequential(IEnumerable<IJob> jobs, ILogger logger, IStateManager stateManager)
    {
        bool allSuccess = true;
        foreach (var job in jobs)
        {
            if (!ExecuteSingle(job, logger, stateManager))
                allSuccess = false;
        }
        return allSuccess;
    }
    
    //From command line arguments
    public bool ExecuteFromCommand(string command, IJobManager manager, ILogger logger, IStateManager stateManager)
    {
        _localization = new LocalizationService();

        var indexes = new HashSet<int>();

        var parts = command.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            //Range : X-Y
            if (trimmed.Contains('-'))
            {
                var bounds = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);

                if (bounds.Length != 2 ||
                    !int.TryParse(bounds[0], out int start) ||
                    !int.TryParse(bounds[1], out int end))
                {
                    Console.WriteLine(_localization.GetString("invalid_choice"));
                    return false;
                }

                if (start > end)
                    (start, end) = (end, start);

                for (int i = start; i <= end; i++)
                    indexes.Add(i);
            }
            //Single index
            else
            {
                if (!int.TryParse(trimmed, out int index))
                {
                    Console.WriteLine(_localization.GetString("invalid_choice"));
                    return false;
                }

                indexes.Add(index);
            }
        }

        //Validation des bornes
        foreach (var index in indexes)
        {
            if (index < 1 || index > manager.Jobs.Count || index > manager.MaxJobs)
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
                return false;
            }
        }

        //Récupération des jobs
        var jobsToExecute = indexes
            .OrderBy(i => i)
            .Select(i => manager.GetJob(i))
            .ToList();

        //Exécution séquentielle, return true if all jobs succeeded
        return ExecuteSequential(jobsToExecute, logger, stateManager);
    }
}