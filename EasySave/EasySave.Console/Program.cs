namespace EasySave.ConsoleApp;

using EasySave.Core.Interfaces;
using EasySave.Core.Services;
using EasySaveLog;

public class Program
{
    public static void Main(string[] args)
    {
        // Global exception handler to prevent crashes from unexpected errors (like USB disconnection)
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler.HandleUnhandledException;
        
        // Services
        var localization = new LocalizationService();
        var jobManager = new JobManager(localization);
        var configManager = new ConfigManager();
        var logger = new Logger();
        var stateManager = new StateManager();
        var backupExecutor = new BackupExecutor();
        var pathValidator = new PathValidator();
        var menuHandler = new MenuHandler(localization, jobManager, configManager, backupExecutor, logger, stateManager, pathValidator);
        
        // Logger init
        logger.Initialize();
        
        // Load all exisiting jobs
        configManager.LoadJobs(jobManager);
        
        // Select mode to run
        if (args.Length > 0)
        {
            backupExecutor.ExecuteFromCommand(args[0], jobManager, logger, stateManager, localization);
            return;
        }
        
        // Show menu to user
        menuHandler.ShowMenu();
    }
}