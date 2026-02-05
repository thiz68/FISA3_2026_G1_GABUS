namespace EasySave.ConsoleApp;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySaveLog;
using System.IO;

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
            Console.WriteLine(_localization.GetString("menu_remove"));    // Option 2
            Console.WriteLine(_localization.GetString("menu_modify"));    // Option 3
            Console.WriteLine(_localization.GetString("menu_list"));      // Option 4
            Console.WriteLine(_localization.GetString("menu_execute"));   // Option 5
            Console.WriteLine(_localization.GetString("menu_language"));  // Option 6
            Console.WriteLine(_localization.GetString("menu_exit"));      // Option 7
            Console.WriteLine();
            Console.WriteLine(_localization.GetString("enter_choice"));

            // Get user choice
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    CreateJob();      // Launch job creation loop
                    break;
                case "2":
                    RemoveJobs();     //Remove a job
                    break;
                case "3":
                    ModifyJobs();     //Modify a job
                    break;
                case "4":
                    ListJobs();       // Show all jobs
                    break;
                case "5":
                    ExecuteBackup();  // Select job and run it
                    break;
                case "6":
                    ChangeLanguage(); // Chenge the language
                    break;
                case "7":             // Exit the program
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
            string name;
            bool alr_exist;
            do
            {
                // Ask user for job name
                Console.Write(_localization.GetString("enter_name"));
                name = Console.ReadLine()?.Trim() ?? "";
                alr_exist = _jobManager.Jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                // Check if job name null
                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine(_localization.GetString("input_is_null"));
                }
                // Check if job name exist
                else if (alr_exist)
                {
                    Console.WriteLine(_localization.GetString("job_name_alr_exist"));
                }
            } while (string.IsNullOrWhiteSpace(name) || alr_exist) ;

            // Verify job name not already exist
            if (_jobManager.Jobs.Any(j =>
                    j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(_localization.GetString("job_name_alr_exist"));
                return;
            }

            string source;
            do
            {
                // Ask user for source path
                Console.Write(_localization.GetString("enter_source"));
                source = Console.ReadLine()?.Trim() ?? "";
                // Check if source path null
                if (string.IsNullOrWhiteSpace(source))
                {
                    Console.WriteLine(_localization.GetString("input_is_null"));
                }
                // Check if source path exist
                else if (!Directory.Exists(source))
                {
                    Console.WriteLine(_localization.GetString("error_invalid_source"));
                }
            } while (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source));

            string target;
            bool valid_path;
            do
            {
                // Ask user for target path
                Console.Write(_localization.GetString("enter_target"));
                target = Console.ReadLine()?.Trim() ?? "";
                valid_path = true;
                // Check if target path null
                if (string.IsNullOrWhiteSpace(target))
                {
                    Console.WriteLine(_localization.GetString("input_is_null"));
                }
                else if (!Path.IsPathRooted(target))
                {
                    valid_path = false;
                    Console.WriteLine(_localization.GetString("error_invalid_target")); 
                }
                
            } while (string.IsNullOrWhiteSpace(target) || !valid_path);

            string type;
            string typeInput;
            do
            {
                // Ask user for save type (full for all files, dif for files that changed)
                Console.Write(_localization.GetString("enter_type"));
                typeInput = Console.ReadLine()?.Trim() ?? "";
                type = typeInput == "1" ? "full" : "diff";
                if (typeInput != "1" && typeInput != "2")
                {
                    Console.WriteLine(_localization.GetString("error_invalid_type"));
                }
            } while (typeInput != "1" && typeInput != "2");

            // Create new job object
            var job = new SaveJob
            {
                Name = name,
                SourcePath = source,
                TargetPath = target,
                Type = type
            };

            // Add job to manager
            _jobManager.AddJob(job);

            // Save config
            _configManager.SaveJobs(_jobManager);

            Console.WriteLine(_localization.GetString("job_created"));
        }
        catch
        {
            Console.WriteLine(_localization.GetString("critical_error"));
        }
    }



    private static void RemoveJobs()
    {
        //Exit if no jobs exist
        if (_jobManager.Jobs.Count == 0)
        {
            Console.WriteLine(_localization.GetString("job_list_empty"));
            return;
        }

        //Display all existing jobs
        ListJobs();

        //Ask user which job to remove (number or name)
        Console.Write(_localization.GetString("job_to_remove"));
        var input = Console.ReadLine()?.Trim();

        //Check if input is empty
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine(_localization.GetString("input_is_null"));
            return;
        }

        // Job to remove (found by index or name)
        IJob? jobToRemove = null;

        //Case 1 : user entered a job number
        if (int.TryParse(input, out int index))
        {
            // Check if index is valid
            if (index < 1 || index > _jobManager.Jobs.Count)
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
                return;
            }

            //Get job by index
            jobToRemove = _jobManager.GetJob(index);
        }
        //Case 2 : user entered a job name
        else
        {
            // Search job by name (case-insensitive)
            jobToRemove = _jobManager.Jobs
                .FirstOrDefault(j => j.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

            //If job does not exist
            if (jobToRemove == null)
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
                return;
            }
        }

        //Remove the selected job
        _jobManager.RemoveJob(jobToRemove.Name);

        //Save updated jobs list to config file
        _configManager.SaveJobs(_jobManager);

        //Confirm job removal
        Console.WriteLine(_localization.GetString("job_removed"));
    }




    private static void ModifyJobs()
    {
        if (_jobManager.Jobs.Count == 0)
        {
            Console.WriteLine(_localization.GetString("job_list_empty"));
            return;
        }
        
        ListJobs();
        
        Console.Write(_localization.GetString("job_to_modify"));
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine(_localization.GetString("input_is_null"));
            return;
        }
        
        IJob? jobToModify = null;
        
        //CASE1 : Index
        if (int.TryParse(input, out int index))
        {
            if (index < 1 || index > _jobManager.Jobs.Count)
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
                return;
            }
            
            jobToModify = _jobManager.GetJob(index);
        }

        //CASE 2: Name
        else
        {
            jobToModify = _jobManager.Jobs.FirstOrDefault(j => j.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

            if (jobToModify == null)
            {
                Console.WriteLine(_localization.GetString("error_not_found"));
                return;
            }
        }

        // Ask new values
        try
        {
            string name;
            bool alr_exist;
            do
            {
                // Ask user for job name
                Console.Write(_localization.GetString("enter_name"));
                name = Console.ReadLine()?.Trim() ?? "";
                alr_exist = _jobManager.Jobs.Any(j => j != jobToModify && j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                // Check if job name null
                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine(_localization.GetString("input_is_null"));
                }
                // Check if job name exist
                else if (alr_exist)
                {
                    Console.WriteLine(_localization.GetString("job_name_alr_exist"));
                }
            } while (string.IsNullOrWhiteSpace(name) || alr_exist);

            // Verify job name not already exist
            if (_jobManager.Jobs.Any(j =>
                    j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(_localization.GetString("job_name_alr_exist"));
                return;
            }

            string source;
            do
            {
                // Ask user for source path
                Console.Write(_localization.GetString("enter_source"));
                source = Console.ReadLine()?.Trim() ?? "";
                // Check if source path null
                if (string.IsNullOrWhiteSpace(source))
                {
                    Console.WriteLine(_localization.GetString("input_is_null"));
                }
                // Check if source path exist
                else if (!Directory.Exists(source))
                {
                    Console.WriteLine(_localization.GetString("error_invalid_source"));
                }
            } while (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source));

            string target;
            bool valid_path;
            do
            {
                // Ask user for target path
                Console.Write(_localization.GetString("enter_target"));
                target = Console.ReadLine()?.Trim() ?? "";
                valid_path = true;
                // Check if target path null
                if (string.IsNullOrWhiteSpace(target))
                {
                    Console.WriteLine(_localization.GetString("input_is_null"));
                }
                else if (!Path.IsPathRooted(target))
                {
                    valid_path = false;
                    Console.WriteLine(_localization.GetString("error_invalid_target"));
                }

            } while (string.IsNullOrWhiteSpace(target) || !valid_path);

            string type;
            string typeInput;
            do
            {
                // Ask user for save type (full for all files, dif for files that changed)
                Console.Write(_localization.GetString("enter_type"));
                typeInput = Console.ReadLine()?.Trim() ?? "";
                type = typeInput == "1" ? "full" : "diff";
                if (typeInput != "1" && typeInput != "2")
                {
                    Console.WriteLine(_localization.GetString("error_invalid_type"));
                }
            } while (typeInput != "1" && typeInput != "2");

            //Apply modifications
            jobToModify.Name = name;
            jobToModify.SourcePath = source;
            jobToModify.TargetPath = target;
            jobToModify.Type = type;

            //Save config
            _configManager.SaveJobs(_jobManager);

            Console.WriteLine(_localization.GetString("job_modified"));

        } catch
        {
            Console.WriteLine(_localization.GetString("critical_error"));
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

        // Loop taht get every jobs info and display it
        for (int i = 0; i < _jobManager.Jobs.Count; i++)
        {
            var job = _jobManager.Jobs[i];
            Console.WriteLine($"{i + 1}. {job.Name}");
            Console.WriteLine($"{_localization.GetString("source")}: {job.SourcePath}");
            Console.WriteLine($"{_localization.GetString("target")}: {job.TargetPath}");
            Console.WriteLine($"{_localization.GetString("type")}: {_localization.GetString(job.Type)}");
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
