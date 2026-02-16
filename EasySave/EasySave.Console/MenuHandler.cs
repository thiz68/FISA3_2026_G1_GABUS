namespace EasySave.Console;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using System.Xml.Linq;

public class MenuHandler
{
    private readonly ILocalizationService _localization;
    private readonly IJobManager _jobManager;
    private readonly ConfigManager _configManager;
    private readonly BackupExecutor _backupExecutor;
    private readonly ILogger _logger;
    private readonly IStateManager _stateManager;
    private readonly PathValidator _pathValidator;
    
    public MenuHandler(ILocalizationService localization, IJobManager jobManager, ConfigManager configManager,
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
            System.Console.Clear();
            // Display menu option in the right language (using keys to link with dictionnary)
            System.Console.WriteLine(_localization.GetString("menu_title"));
            System.Console.WriteLine();
            System.Console.WriteLine(_localization.GetString("menu_create"));    // Option 1
            System.Console.WriteLine(_localization.GetString("menu_remove"));    // Option 2
            System.Console.WriteLine(_localization.GetString("menu_modify"));    // Option 3
            System.Console.WriteLine(_localization.GetString("menu_list"));      // Option 4
            System.Console.WriteLine(_localization.GetString("menu_execute"));   // Option 5
            System.Console.WriteLine(_localization.GetString("menu_language"));  // Option 6
            System.Console.WriteLine(_localization.GetString("menu_log_format"));         // Option 7
            System.Console.WriteLine(_localization.GetString("menu_business_software"));  // Option 8
            System.Console.WriteLine(_localization.GetString("menu_exit"));               // Option 9
            System.Console.WriteLine();
            System.Console.Write("> ");
            var choice = System.Console.ReadLine()?.Trim();
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
                    ChangeLogFormat();
                    break;
                case "8":
                    ChangeBusinessSoftware();
                    break;
                case "9":
                    System.Console.WriteLine(_localization.GetString("goodbye"));
                    _configManager.SaveJobs(_jobManager);
                    return;
                default:
                    System.Console.WriteLine(_localization.GetString("invalid_choice"));
                    System.Console.WriteLine(_localization.GetString("press_to_continue"));
                    System.Console.ReadKey();
                    break;
            }
        }
    }


    private void CreateJob()
    {
        System.Console.Clear();
        string name = null;
        string source = null;
        string target = null;
        string type = null;

        // Add name
        while (true)
        {
            System.Console.Write(_localization.GetString("enter_name"));
            name = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                System.Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (_jobManager.Jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                System.Console.WriteLine(_localization.GetString("job_name_alr_exist"));
            }
            else
            {
                break;
            }

        }

        // Add target
        while (true)
        {
            System.Console.Write(_localization.GetString("enter_source"));
            source = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                System.Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (!_pathValidator.IsSourceValid(source))
            {
                System.Console.WriteLine(_localization.GetString("error_invalid_source"));
            }
            else
            {
                break;
            }

        }

        // Add target
        while (true)
        {
            System.Console.Write(_localization.GetString("enter_target"));
            target = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                System.Console.WriteLine(_localization.GetString("input_is_null"));
            }
            else if (!_pathValidator.IsTargetValid(target))
            {
                System.Console.WriteLine(_localization.GetString("error_invalid_target"));
            }
            else
            {
                break;
            }

        }

        // Add type
        while (true)
        {
            System.Console.Write(_localization.GetString("enter_type"));
            type = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(type))
            {
                System.Console.WriteLine(_localization.GetString("input_is_null"));
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
                System.Console.WriteLine(_localization.GetString("error_invalid_type"));
            }
        }

        // Create job
        var job = new SaveJob { Name = name, SourcePath = source, TargetPath = target, Type = type };
        try
        {
            _jobManager.AddJob(job);
            _configManager.SaveJobs(_jobManager);
            System.Console.WriteLine(_localization.GetString("job_created"));
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }

        System.Console.WriteLine(_localization.GetString("press_to_continue"));
        System.Console.ReadKey();
    }



    private void RemoveJob()
    {
        System.Console.Clear();
        bool listJobs = ListJobs(true, true);
        if (listJobs == false)
        {
            return;
        }
        System.Console.Write(_localization.GetString("job_to_remove"));
        var input = System.Console.ReadLine()?.Trim();
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
                System.Console.WriteLine(_localization.GetString("job_removed"));
            }
            // User entered a name
            else if (_jobManager.Jobs.FirstOrDefault(j => j.Name.Equals(input, StringComparison.OrdinalIgnoreCase)) != null)
            {
                var job = _jobManager.GetJob(input);
                _jobManager.RemoveJob(job.Name);
                _configManager.SaveJobs(_jobManager);
                System.Console.WriteLine(_localization.GetString("job_removed"));
            }
            else
            {
                System.Console.WriteLine(_localization.GetString("error_not_found"));
            }

        }
        catch
        {
            System.Console.WriteLine(_localization.GetString("error_not_found"));
        }
        System.Console.WriteLine(_localization.GetString("press_to_continue"));
        System.Console.ReadKey();
    }


    private void ModifyJob()
    {
        System.Console.Clear();
        bool listJobs = ListJobs(true, true);
        if (listJobs == false)
        {
            return;
        }
        System.Console.Write(_localization.GetString("job_to_modify"));
        var input = System.Console.ReadLine()?.Trim();
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
                System.Console.WriteLine(_localization.GetString("error_not_found"));
            }

            if (job != null)
            {

                // Edit name
                while (true)
                {
                    System.Console.Write(_localization.GetString("enter_name"));
                    var newName = System.Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        break;
                    }
                    else if (_jobManager.Jobs.Any(j => j != job && j.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                    {
                        System.Console.WriteLine(_localization.GetString("job_name_alr_exist"));
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
                    System.Console.Write(_localization.GetString("enter_source"));
                    var newSource = System.Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(newSource))
                    {
                        break;
                    }
                    else if (!_pathValidator.IsSourceValid(newSource))
                    {
                        System.Console.WriteLine(_localization.GetString("error_invalid_source"));
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
                    System.Console.Write(_localization.GetString("enter_target"));
                    var newTarget = System.Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(newTarget))
                    {
                        break;
                    }
                    else if (!_pathValidator.IsTargetValid(newTarget))
                    {
                        System.Console.WriteLine(_localization.GetString("error_invalid_target"));
                    }
                    else
                    {
                        job.TargetPath = newTarget;
                        break;
                    }

                }

                System.Console.Write(_localization.GetString("enter_type"));
                var typeInput = System.Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(typeInput))
                    job.Type = typeInput == "2" ? "diff" : "full";

                _configManager.SaveJobs(_jobManager);
                System.Console.WriteLine(_localization.GetString("job_modified"));
                System.Console.WriteLine(_localization.GetString("press_to_continue"));
                System.Console.ReadKey();
            }
        }
        catch
        {
            System.Console.WriteLine(_localization.GetString("error_not_found"));
        }
    }

    private bool ListJobs(bool withNumbers = false, bool bypassPTC = false)
    {
        System.Console.Clear();
        if (_jobManager.Jobs.Count == 0)
        {
            System.Console.WriteLine(_localization.GetString("job_list_empty"));
            System.Console.WriteLine(_localization.GetString("press_to_continue"));
            System.Console.ReadKey();
            return false;
        }

        for (int i = 0; i < _jobManager.Jobs.Count; i++)
        {
            var job = _jobManager.Jobs[i];
            if (withNumbers)
                System.Console.Write($"{i + 1}. ");
            System.Console.WriteLine($"{job.Name}");
            System.Console.WriteLine($"  {_localization.GetString("source")}: {job.SourcePath}");
            System.Console.WriteLine($"  {_localization.GetString("target")}: {job.TargetPath}");
            System.Console.WriteLine($"  {_localization.GetString("type")}: {(job.Type == "full" ? _localization.GetString("full") : _localization.GetString("diff"))}");
            System.Console.WriteLine();
        }
        if (bypassPTC == false)
        {
            System.Console.WriteLine(_localization.GetString("press_to_continue"));
            System.Console.ReadKey();
        }

        return true;

    }

    private void ExecuteBackup()
    {
        System.Console.Clear();
        bool listJobs = ListJobs(true, true);
        if (listJobs == false)
        {
            return;
        }
        System.Console.WriteLine(_localization.GetString("enter_job_number"));
        System.Console.WriteLine("Ex : 1 | 2;5 | 2-4");
        var input = System.Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
            return;

        var formatter = new BackupListFormatter(_localization);
        var (success, message, jobs) = formatter.FormatJobList(input, _jobManager);

        // Executer les backup si la liste entree par utilisateur est correcte
        if (success == false)
        {
            System.Console.WriteLine(message);
            System.Console.WriteLine(_localization.GetString("press_to_continue"));
            System.Console.ReadKey();
        }
        else
        {
            // Load settings to get business software name
            var settings = _configManager.LoadSettings();
            var checker = new BusinessSoftwareChecker();

            // Check BEFORE starting: if business software is running, block the backup
            if (!string.IsNullOrWhiteSpace(settings.BusinessSoftware) &&
                checker.IsBusinessSoftwareRunning(settings.BusinessSoftware))
            {
                System.Console.WriteLine(_localization.GetString("business_software_detected"));
                System.Console.WriteLine(_localization.GetString("press_to_continue"));
                System.Console.ReadKey();
                return;
            }

            System.Console.WriteLine(_localization.GetString("backup_started"));

            // Create callback to check if business software starts during backup
            Func<bool>? shouldStop = null;
            if (!string.IsNullOrWhiteSpace(settings.BusinessSoftware))
            {
                shouldStop = () => checker.IsBusinessSoftwareRunning(settings.BusinessSoftware);
            }

            var result = _backupExecutor.ExecuteSequential(jobs, _logger, _stateManager, shouldStop);

            // Show specific message if stopped due to business software
            if (result == "backup_failed" && shouldStop?.Invoke() == true)
            {
                System.Console.WriteLine(_localization.GetString("business_software_stopped"));
            }
            else
            {
                System.Console.WriteLine(_localization.GetString(result));
            }
            System.Console.WriteLine(_localization.GetString("press_to_continue"));
            System.Console.ReadKey();
        }

    }

    private void ChangeLanguage()
    {
        System.Console.Clear();
        System.Console.WriteLine("1. English");
        System.Console.WriteLine("2. Francais");
        System.Console.Write("> ");
        var choice = System.Console.ReadLine()?.Trim();

        // Set language
        switch (choice)
        {
            case "1":
                _localization.SetLanguage("en");
                // Save language preference
                var settingsEn = _configManager.LoadSettings();
                settingsEn.Language = "en";
                _configManager.SaveSettings(settingsEn);
                break;
            case "2":
                _localization.SetLanguage("fr");
                // Save language preference
                var settingsFr = _configManager.LoadSettings();
                settingsFr.Language = "fr";
                _configManager.SaveSettings(settingsFr);
                break;
        }
        System.Console.WriteLine(_localization.GetString("press_to_continue"));
        System.Console.ReadKey();
    }

    private void ChangeLogFormat()
    {
        System.Console.Clear();
        System.Console.WriteLine(_localization.GetString("log_format") + ": " + _logger.GetCurrentLogFormat().ToUpper());
        System.Console.WriteLine("1. JSON");
        System.Console.WriteLine("2. XML");
        System.Console.WriteLine();
        System.Console.Write("> ");

        var choice = System.Console.ReadLine()?.Trim();

        string newFormat = null;
        switch (choice)
        {
            case "1":
                newFormat = "json";
                break;
            case "2":
                newFormat = "xml";
                break;
            default:
                System.Console.WriteLine(_localization.GetString("invalid_choice"));
                System.Console.WriteLine(_localization.GetString("press_to_continue"));
                System.Console.ReadKey();
                return;
        }

        _logger.SetLogFormat(newFormat);

        // Save logs format in settings
        var settings = _configManager.LoadSettings();
        settings.LogFormat = newFormat;
        _configManager.SaveSettings(settings);

        System.Console.WriteLine(_localization.GetString("log_format_changed") + " " + newFormat.ToUpper());
        System.Console.WriteLine(_localization.GetString("press_to_continue"));
        System.Console.ReadKey();
    }

    private void ChangeBusinessSoftware()
    {
        System.Console.Clear();
        var settings = _configManager.LoadSettings();

        System.Console.WriteLine(_localization.GetString("business_software") + ": " +
            (string.IsNullOrWhiteSpace(settings.BusinessSoftware) ? "-" : settings.BusinessSoftware));
        System.Console.WriteLine();
        System.Console.WriteLine(_localization.GetString("enter_business_software"));
        System.Console.Write("> ");

        var input = System.Console.ReadLine()?.Trim();

        settings.BusinessSoftware = input ?? string.Empty;
        _configManager.SaveSettings(settings);

        System.Console.WriteLine(_localization.GetString("settings_saved"));
        System.Console.WriteLine(_localization.GetString("press_to_continue"));
        System.Console.ReadKey();
    }
}
