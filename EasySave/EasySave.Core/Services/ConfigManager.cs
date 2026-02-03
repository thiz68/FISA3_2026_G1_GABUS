namespace EasySave.Core.Services;

using System.Text.Json;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

//Saving and loading jobs configurations to a JSON file
public class ConfigManager : IConfigManager
{
    //Path to configuration file
    private readonly string _configFilePath;
    
    //Constructor
    public ConfigManager()
    {
        //Get AppData folder
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        //Create app folder
        var configDir = Path.Combine(appData, "EasySave");
        
        Directory.CreateDirectory(configDir);

        //Set path to config file
        _configFilePath = Path.Combine(configDir, "config.json");
    }
    
    //Load jobs from config file, send to job manager
    public void LoadJobs(IJobManager manager)
    {
        if (!File.Exists(_configFilePath))
            return;
        
        var json = File.ReadAllText(_configFilePath);
        var jobs = JsonSerializer.Deserialize<List<SaveJob>>(json);

        if (jobs == null)
            return;
        
        foreach (var job in jobs)
            manager.AddJob(job);
    }
}