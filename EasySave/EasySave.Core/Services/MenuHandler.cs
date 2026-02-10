namespace EasySave.ConsoleApp;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;

public class MenuHandler
{
    private readonly ILocalizationService _localization;
    private readonly IJobManager _jobManager;
    private readonly IConfigManager _configManager;
    private readonly BackupExecutor _backupExecutor;
    private readonly ILogger _logger;
    private readonly IStateManager _stateManager;
    private readonly PathValidator _pathValidator;
    public MenuHandler(ILocalizationService localization, IJobManager jobManager, IConfigManager configManager,
    
        BackupExecutor backupExecutor, ILogger logger, IStateManager stateManager, PathValidator pathValidator)
    {
        _localization = localization;
        _jobManager = jobManager;
        _configManager = configManager;
        _backupExecutor = backupExecutor;
        _logger = logger;
        _stateManager = stateManager;
        _pathValidator = pathValidator;
    }
    
    public void ShowMenu()
    {
        while (true)
        {
            // Clear console
            Console.Clear();
            // Display menu option in the right language (using keys to link with dictionnary)
            Console.WriteLine(_localization.GetString("menu_title"));
            Console.WriteLine();
            Console.WriteLine(_localization.GetString("menu_create"));    // Option 1
            Console.WriteLine(_localization.GetString("menu_remove"));    // Option 2
            Console.WriteLine(_localization.GetString("menu_modify"));    // Option 3
            Console.WriteLine(_localization.GetString("menu_list"));      // Option 4
            Console.WriteLine(_localization.GetString("menu_execute"));   // Option 5
            Console.WriteLine(_localization.GetString("menu_language"));  // Option 6
            Console.WriteLine(_localization.GetString("menu_exit"));      // Option 7
            Console.WriteLine();
            Console.Write("> ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    CreateJob();
                    break;
                case "2":
                    RemoveJob();
                    break;
                case "3":
                    ModifyJob();
                    break;
                case "4":
                    ListJobs();
                    break;
                case "5":
                    ExecuteBackup();
                    break;
                case "6":
                    ChangeLanguage();
                    break;
                case "7":
                    Console.WriteLine(_localization.GetString("goodbye"));
                    _configManager.SaveJobs(_jobManager);
                    return;
                default:
                    Console.WriteLine(_localization.GetString("invalid_choice"));
                    Console.WriteLine(_localization.GetString("press_to_continue"));
                    Console.ReadKey();
                    break;
            }
        }
    }
    
    private void CreateJob()
    {
        Console.Clear();
        Console.WriteLine(_localization.GetString("enter_name"));
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine(_localization.GetString("error_invalid_name"));
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine(_localization.GetString("enter_source"));
        var source = Console.ReadLine()?.Trim();
        if (!_pathValidator.IsSourceValid(source))
        {
            Console.WriteLine(_localization.GetString("error_invalid_source"));
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine(_localization.GetString("enter_target"));
        var target = Console.ReadLine()?.Trim();
        if (!_pathValidator.IsTargetValid(target))
        {
            Console.WriteLine(_localization.GetString("error_invalid_target"));
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine(_localization.GetString("enter_type"));
        var typeInput = Console.ReadLine()?.Trim();
        string type = typeInput == "2" ? "diff" : "full";
        var job = new SaveJob { Name = name, SourcePath = source!, TargetPath = target!, Type = type };
        try
        {
            _jobManager.AddJob(job);
            _configManager.SaveJobs(_jobManager);
            Console.WriteLine(_localization.GetString("job_created"));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
        
        Console.WriteLine(_localization.GetString("press_to_continue"));
        Console.ReadKey();
    }
    
    private void RemoveJob()
    {
        Console.Clear();
        ListJobs(true);
        Console.WriteLine(_localization.GetString("job_to_remove"));
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;
        try
        {
            if (int.TryParse(input, out int index) && index >= 1 && index <= _jobManager.Jobs.Count)
            {
                var job = _jobManager.GetJob(index);
                _jobManager.RemoveJob(job.Name);
            }
            else
            {
                _jobManager.RemoveJob(input);
            }
            _configManager.SaveJobs(_jobManager);
            Console.WriteLine(_localization.GetString("job_removed"));
        }
        catch
        {
            Console.WriteLine(_localization.GetString("error_not_found"));
        }
        Console.WriteLine(_localization.GetString("press_to_continue"));
        Console.ReadKey();
    }
    
    private void ModifyJob()
    {
        Console.Clear();
        ListJobs(true);
        Console.WriteLine(_localization.GetString("job_to_modify"));
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;
        
        IJob job;
        try
        {
            if (int.TryParse(input, out int index) && index >= 1 && index <= _jobManager.Jobs.Count)
            {
                job = _jobManager.GetJob(index);
            }
            else
            {
                job = _jobManager.GetJob(input);
            }
        }
        catch
        {
            Console.WriteLine(_localization.GetString("error_not_found"));
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine(_localization.GetString("enter_name"));
        var newName = Console.ReadLine()?.Trim();
        
        if (!string.IsNullOrWhiteSpace(newName))
            job.Name = newName;
        
        Console.WriteLine(_localization.GetString("enter_source"));
        var newSource = Console.ReadLine()?.Trim();
        
        if (!string.IsNullOrWhiteSpace(newSource) && _pathValidator.IsSourceValid(newSource))
            job.SourcePath = newSource;
        else if (!string.IsNullOrWhiteSpace(newSource))
        {
            Console.WriteLine(_localization.GetString("error_invalid_source"));
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine(_localization.GetString("enter_target"));
        var newTarget = Console.ReadLine()?.Trim();
        
        if (!string.IsNullOrWhiteSpace(newTarget) && _pathValidator.IsTargetValid(newTarget))
            job.TargetPath = newTarget;
        else if (!string.IsNullOrWhiteSpace(newTarget))
        {
            Console.WriteLine(_localization.GetString("error_invalid_target"));
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine(_localization.GetString("enter_type"));
        var typeInput = Console.ReadLine()?.Trim();
        
        if (!string.IsNullOrWhiteSpace(typeInput))
            job.Type = typeInput == "2" ? "diff" : "full";
        
        _configManager.SaveJobs(_jobManager);
        Console.WriteLine(_localization.GetString("job_modified"));
        Console.WriteLine(_localization.GetString("press_to_continue"));
        Console.ReadKey();
    }
    
    private void ListJobs(bool withNumbers = false)
    {
        Console.Clear();
        if (_jobManager.Jobs.Count == 0)
        {
            Console.WriteLine(_localization.GetString("job_list_empty"));
            Console.WriteLine(_localization.GetString("press_to_continue"));
            Console.ReadKey();
            return;
        }
        
        for (int i = 0; i < _jobManager.Jobs.Count; i++)
        {
            var job = _jobManager.Jobs[i];
            if (withNumbers)
                Console.Write($"{i + 1}. ");
            Console.WriteLine($"{job.Name}");
            Console.WriteLine($"  {_localization.GetString("source")}: {job.SourcePath}");
            Console.WriteLine($"  {_localization.GetString("target")}: {job.TargetPath}");
            Console.WriteLine($"  {_localization.GetString("type")}: {(job.Type == "full" ? _localization.GetString("full") : _localization.GetString("diff"))}");
            Console.WriteLine();
        }
        
        Console.WriteLine(_localization.GetString("press_to_continue"));
        Console.ReadKey();
    }
    
    private void ExecuteBackup()
    {
        Console.Clear();
        ListJobs(true);
        Console.WriteLine(_localization.GetString("enter_job_number"));
        var input = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrWhiteSpace(input))
            return;
        
        Console.WriteLine(_localization.GetString("backup_started"));
        var success = _backupExecutor.ExecuteFromCommand(input, _jobManager, _logger, _stateManager, _localization);
        Console.WriteLine(success ? _localization.GetString("backup_completed") : _localization.GetString("backup_failed"));
        Console.WriteLine(_localization.GetString("press_to_continue"));
        Console.ReadKey();
    }
    
    private void ChangeLanguage()
    {
        Console.Clear();
        Console.WriteLine("1. English");
        Console.WriteLine("2. Français");
        Console.Write("> ");
        var choice = Console.ReadLine()?.Trim();
        
        // Set language
        switch (choice)
        {
            case "1":
                _localization.SetLanguage("en");
                break;
            case "2":
                _localization.SetLanguage("fr");
                break;
        }
        Console.WriteLine(_localization.GetString("press_to_continue"));
        Console.ReadKey();
    }
}