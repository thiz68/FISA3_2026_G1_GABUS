namespace EasySave.Core.Services;

using System.Text.Json;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

//Saving and loading jobs configurations to a JSON file
public class ConfigManager : IConfigManager
{
    //Path to configuration file
    private readonly string _configFilePath;
    private readonly string _settingsFilePath;

    //Constructor
    public ConfigManager()
    {
        //Get the application's directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        //Set path to config file
        _configFilePath = Path.Combine(appDirectory, "config.json");
        _settingsFilePath = Path.Combine(appDirectory, "settings.json");
    }

    //Load jobs from config file, send to job manager
    public void LoadJobs(IJobManager manager)
    {
        if (!File.Exists(_configFilePath))
            return;

        // Try to read config file, handle errors if file/drive is unavailable
        try
        {
            var json = File.ReadAllText(_configFilePath);
            var jobs = JsonSerializer.Deserialize<List<SaveJob>>(json);

            if (jobs == null)
                return;

            foreach (var job in jobs)
                manager.AddJob(job);
        }
        catch (IOException)
        {
            // Could not read config file, start with empty job list
        }
    }

    //Save jobs
    public void SaveJobs(IJobManager manager)
    {
        //JSON output
        var options = new JsonSerializerOptions { WriteIndented = true };

        var json = JsonSerializer.Serialize(manager.Jobs, options);

        // Try to write config file, handle errors if drive becomes unavailable
        try
        {
            //Write JSON to config file
            File.WriteAllText(_configFilePath, json);
        }
        catch (IOException)
        {
            // Could not save config, changes will be lost
        }
    }

    // Load application settings
    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    // Save application settings
    public void SaveSettings(AppSettings settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);

        try
        {
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (IOException)
        {
            // Could not save settings
        }
    }
}
