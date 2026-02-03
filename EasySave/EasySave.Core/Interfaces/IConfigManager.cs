namespace EasySave.Core.Interfaces;

// Interface for loading/saving job configurations
public interface IConfigManager
{
    // Load jobs from config file into the manager
    void LoadJobs(IJobManager manager);

    // Save jobs from the manager to config file
    void SaveJobs(IJobManager manager);
}
