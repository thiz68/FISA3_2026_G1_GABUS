using System.Text.Json;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

namespace EasySave.Core.Services;

public class ConfigManager : IConfigManager
{
    private readonly string _configFilePath;
    private readonly string _settingsFilePath;

    public ConfigManager()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        _configFilePath = Path.Combine(appDirectory, "config.json");
        _settingsFilePath = Path.Combine(appDirectory, "settings.json");
    }

    public void LoadJobs(IJobManager manager)
    {
        if (!File.Exists(_configFilePath))
            return;

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
            // Could not read config file
        }
    }

    public void SaveJobs(IJobManager manager)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(manager.Jobs, options);

        try
        {
            File.WriteAllText(_configFilePath, json);
        }
        catch (IOException)
        {
            // Could not save config
        }
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

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