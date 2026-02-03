namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;

//Execution of backup jobs
public class BackupExecutor
{
    private readonly FileBackupService _fileBackupService;

    public BackupExecutor()
    {
        _fileBackupService = new FileBackupService();
    }

    //Single Job    
    public void ExecuteSingle(IJob job, ILogger logger, IStateManager stateManager)
    {
        //Initialize the state as Active
        var state = new JobState { State = "Active" };
        stateManager.UpdateJobState(job, state);
        
        //Copy all files from source to target
        _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager);
        
        //Mark job as completed
        state.State = "Completed";
        state.Progression = 100;
        stateManager.UpdateJobState(job, state);
    }
    
    //Multiple jobs
    public void ExecuteSequential(IEnumerable<IJob> jobs, ILogger logger, IStateManager stateManager)
    {
        foreach (var job in jobs)
        {
            ExecuteSingle(job, logger, stateManager);
        }
    }
    
    //From command line arguments
    public void ExecuteFromCommand(string command, IJobManager manager, ILogger logger, IStateManager stateManager)
    {
        var jobsToExecute = new List<IJob>();

        //Check range format ("1-3")
        if (command.Contains('-'))
        {
            var parts = command.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
            {
                for (int i = start; i <= end; i++)
                {
                    if (i >= 1 && i <= manager.Jobs.Count)
                        jobsToExecute.Add(manager.GetJob(i));
                }
            }
        }
        //Check for selection format ("1;3")
        else if (command.Contains(';'))
        {
            var parts = command.Split(';');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int index) && index >= 1 && index <= manager.Jobs.Count)
                    jobsToExecute.Add(manager.GetJob(index));
            }
        }
        //Single job ("1")
        else if (int.TryParse(command, out int index) && index >= 1 && index <= manager.Jobs.Count)
        {
            jobsToExecute.Add(manager.GetJob(index));
        }

        //Execute all selected jobs
        ExecuteSequential(jobsToExecute, logger, stateManager);
    }
}