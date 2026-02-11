namespace EasySave.ConsoleApp;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using System.Xml.Linq;

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
        string name = null;
        string source = null;
        string target = null;
        string type = null;

        // Add name
        while (true)
        {
            Console.Write(_localization.GetString("enter_name"));
            name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (_jobManager.Jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(_localization.GetString("job_name_alr_exist"));
            }
            else
            {
                break;
            }

        }

        // Add target
        while (true)
        {
            Console.Write(_localization.GetString("enter_source"));
            source = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (!_pathValidator.IsSourceValid(source))
            {
                Console.WriteLine(_localization.GetString("error_invalid_source"));
            }
            else
            {
                break;
            }

        }

        // Add target
        while (true)
        {
            Console.Write(_localization.GetString("enter_target"));
            target = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (!_pathValidator.IsTargetValid(target))
            {
                Console.WriteLine(_localization.GetString("error_invalid_target"));
            }
            else
            {
                break;
            }

        }

        // Add type
        while (true)
        {
            Console.Write(_localization.GetString("enter_type"));
            type = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(type))
            {
                Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (type == "1" || type == "full")
            {
                type = "full";
                break;
            }
            else if (type == "2" || type == "diff")
            {
                type = "diff";
                break;
            }
            else
            {
                Console.WriteLine(_localization.GetString("error_invalid_type"));
            }
        }

        // Create job
        var job = new SaveJob { Name = name, SourcePath = source, TargetPath = target, Type = type };
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
        ListJobs(true, true);
        Console.Write(_localization.GetString("job_to_remove"));
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;
        try
        {
            // User entered a number
            if (int.TryParse(input, out int index) && index >= 1 && index <= _jobManager.Jobs.Count)
            {
                var job = _jobManager.GetJob(index);
                _jobManager.RemoveJob(job.Name);
                _configManager.SaveJobs(_jobManager);
                Console.WriteLine(_localization.GetString("job_removed"));
            }
            // User entered a name
            else if (_jobManager.Jobs.FirstOrDefault(j => j.Name.Equals(input, StringComparison.OrdinalIgnoreCase)) != null)
            {
                var job = _jobManager.GetJob(input);
                _jobManager.RemoveJob(job.Name);
                _configManager.SaveJobs(_jobManager);
                Console.WriteLine(_localization.GetString("job_removed"));
            }
            else
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
            }
            
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
        ListJobs(true, true);
        Console.Write(_localization.GetString("job_to_modify"));
        var input = Console.ReadLine()?.Trim();
        IJob? job = null;
        if (string.IsNullOrWhiteSpace(input))
            return;
        try
        {
            // User entered a number
            if (int.TryParse(input, out int index) && index >= 1 && index <= _jobManager.Jobs.Count)
            {
                job = _jobManager.GetJob(index);
            }
            // User entered a name
            else if (_jobManager.Jobs.FirstOrDefault(j => j.Name.Equals(input, StringComparison.OrdinalIgnoreCase)) != null)
            {
                job = _jobManager.GetJob(input);
            }
            else
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
            }

            if (job != null) {
                
                // Edit name
                while (true)
                {
                    Console.Write(_localization.GetString("enter_name"));
                    var newName = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        break;
                    }
                    else if (_jobManager.Jobs.Any(j => j != job && j.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine(_localization.GetString("job_name_alr_exist"));
                    }
                    else
                    {
                        job.Name = newName;
                        break;
                    }

                }

                // Edit target
                while (true)
                {
                    Console.Write(_localization.GetString("enter_source"));
                    var newSource = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(newSource)) {
                        break;
                    }
                    else if (!_pathValidator.IsSourceValid(newSource)){
                        Console.WriteLine(_localization.GetString("error_invalid_source"));
                    }
                    else
                    {
                        job.SourcePath = newSource;
                        break;
                    }
                     
                }

                // Edit target
                while (true)
                {
                    Console.Write(_localization.GetString("enter_target"));
                    var newTarget = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(newTarget))
                    {
                        break;
                    }
                    else if (!_pathValidator.IsTargetValid(newTarget))
                    {
                        Console.WriteLine(_localization.GetString("error_invalid_target"));
                    }
                    else
                    {
                        job.TargetPath = newTarget;
                        break;
                    }

                }

                Console.Write(_localization.GetString("enter_type"));
                var typeInput = Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(typeInput))
                    job.Type = typeInput == "2" ? "diff" : "full";

                _configManager.SaveJobs(_jobManager);
                Console.WriteLine(_localization.GetString("job_modified"));
                Console.WriteLine(_localization.GetString("press_to_continue"));
                Console.ReadKey();
            }
        }
        catch
        {
            Console.WriteLine(_localization.GetString("error_not_found"));
        }
    }
    
    private void ListJobs(bool withNumbers = false, bool bypassPTC = false)
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
        if (bypassPTC == false)
        {
            Console.WriteLine(_localization.GetString("press_to_continue"));
            Console.ReadKey();
        }
        
    }
    
    private void ExecuteBackup()
    {
        Console.Clear();
        ListJobs(true, true);
        Console.WriteLine(_localization.GetString("enter_job_number"));
        Console.WriteLine("Ex : 1 | 2;5 | 2-4");
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