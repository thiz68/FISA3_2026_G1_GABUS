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
    public void ExecuteSingle(IJob job, ILogger logger, IStateManager stateManager)
    {
        _localization = new LocalizationService();

        //Initialize the state as Active
        var state = new JobState { State = _localization.GetString("active") };
        stateManager.UpdateJobState(job, state);
        
        //Copy all files from source to target
        _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager);
        
        //Mark job as completed
        state.State = _localization.GetString("completed");
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
        
    }
}