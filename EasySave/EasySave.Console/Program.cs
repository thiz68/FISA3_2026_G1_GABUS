namespace EasySave.ConsoleApp;

using EasySave.Core.Enums;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySaveLog;

// Stop conflict with EasySaveConsole namespace
using Console = System.Console;

public class Program
{
  
    private static IJobManager _jobManager = null!;           // Manages backup jobs (add, remove, list)
    private static IConfigManager _configManager = null!;     // Saves and loads jobs to and from config file
    private static ILocalizationService _localization = null!; // Service that switch the language
    private static ILogger _logger = null!;                   // Writes log files
    private static IStateManager _stateManager = null!;       // Monitor the state of the current job
    private static BackupExecutor _backupExecutor = null!;    // Do the backup


    public static void Main(string[] args)
    {
        // Services
        _jobManager = new JobManager();
        _configManager = new ConfigManager();
        _localization = new LocalizationService();
        _logger = new Logger();
        _stateManager = new StateManager();
        _backupExecutor = new BackupExecutor();

        // Logger init
        _logger.Initialize();

        // Load all exisiting jobs
        _configManager.LoadJobs(_jobManager);

        // Select mode to run
        if (args.Length > 0)
        {
            ExecuteCommand(args[0]);
            return;
        }

        // Show menu to user
        ShowMenu();
    }

    // Infinite loop that displays menu
    private static void ShowMenu()
    {
        while (true)
        {
            // Clear console 
            Console.Clear();

            // Display menu option in the right language (using keys to link with dictionnary)
            Console.WriteLine(_localization.GetString("menu_title"));
            Console.WriteLine();
            Console.WriteLine(_localization.GetString("menu_create"));    // Option 1
            Console.WriteLine(_localization.GetString("menu_list"));      // Option 2
            Console.WriteLine(_localization.GetString("menu_execute"));   // Option 3
            Console.WriteLine(_localization.GetString("menu_language"));  // Option 4
            Console.WriteLine(_localization.GetString("menu_exit"));      // Option 5
            Console.WriteLine();
            Console.Write(_localization.GetString("enter_choice"));

            // Get user choice
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    CreateJob();      // Launch job creation loop
                    break;
                case "2":
                    ListJobs();       // Show all jobs
                    break;
                case "3":
                    ExecuteBackup();  // Select job and run it
                    break;
                case "4":
                    ChangeLanguage(); // Chenge the language
                    break;
                case "5":             // Exit the program
                    return;    
                default:
                    // If wrong input :
                    Console.WriteLine(_localization.GetString("invalid_choice"));
                    break;
            }

            Console.WriteLine();
            Console.WriteLine(_localization.GetString("press_to_continue"));
            Console.ReadKey(); // Wait user to press a key
        }
    }

    // Create Job function
    private static void CreateJob()
    {
        try
        {
            // Ask user for job name
            Console.Write(_localization.GetString("enter_name"));
            var name = Console.ReadLine()?.Trim() ?? "";

            // Ask user for source path
            Console.Write(_localization.GetString("enter_source"));
            var source = Console.ReadLine()?.Trim() ?? "";

            // Ask user for target path
            Console.Write(_localization.GetString("enter_target"));
            var target = Console.ReadLine()?.Trim() ?? "";

            // Ask user for save type (full for all files, differential for files that changed)
            Console.Write(_localization.GetString("enter_type"));
            var typeInput = Console.ReadLine()?.Trim();
            var type = typeInput == "2" ? SaveType.Differential : SaveType.Full;

            // Create a job with all infso
            var job = new SaveJob
            {
                Name = name,
                SourcePath = source,
                TargetPath = target,
                Type = type
            };

            // Add the job to job list + save in config file
            _jobManager.AddJob(job);
            _configManager.SaveJobs(_jobManager);

            Console.WriteLine(_localization.GetString("job_created"));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"{_localization.GetString("error_max_jobs")}: {ex.Message}");
        }
    }

    // Display jobs function
    private static void ListJobs()
    {
        // Exit if no jobs in list
        if (_jobManager.Jobs.Count == 0)
        {
            Console.WriteLine(_localization.GetString("job_list_empty"));
            return;
        }

        Console.WriteLine();

        // Looptaht get every jobs info and display it
        for (int i = 0; i < _jobManager.Jobs.Count; i++)
        {
            var job = _jobManager.Jobs[i];
            Console.WriteLine($"{i + 1}. {job.Name}");
            Console.WriteLine($"   Source: {job.SourcePath}");
            Console.WriteLine($"   Target: {job.TargetPath}");
            Console.WriteLine($"   Type: {job.Type}");
            Console.WriteLine();
        }
    }

    // Get jobs to execute function
    private static void ExecuteBackup()
    {
        // Show jobs in jobs list
        ListJobs();

        // Stop if no jobs in list
        if (_jobManager.Jobs.Count == 0) return;

        // Ask user for jobs he wants to run
        Console.WriteLine(_localization.GetString("enter_job_number"));
        Console.WriteLine("Ex: 1 | 2-5 | 1;3;4");
        Console.Write("> ");

        // Read and execute the command
        var command = Console.ReadLine()?.Trim() ?? "";
        ExecuteCommand(command);
    }

    // Execute the selected jobs
    private static void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || _jobManager.Jobs.Count == 0)
        {
            Console.WriteLine(_localization.GetString("job_list_empty")); //exit if wrong input
            return;
        }

        // Start the backup
        Console.WriteLine(_localization.GetString("backup_started"));
        _backupExecutor.ExecuteFromCommand(command, _jobManager, _logger, _stateManager);
        Console.WriteLine(_localization.GetString("backup_completed"));
    }

    // Change language function
    private static void ChangeLanguage()
    {
        // Show language options
        Console.WriteLine("1. English");
        Console.WriteLine("2. Francais");
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
    }
}
