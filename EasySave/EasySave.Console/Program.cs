namespace EasySave.Console;

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
        var logger = new Logger(configManager);
        var stateManager = new StateManager();
        var backupExecutor = new BackupExecutor(localization);
        var pathValidator = new PathValidator();
        var menuHandler = new MenuHandler(localization, jobManager, configManager, backupExecutor, logger, stateManager, pathValidator);

        // Logger init
        logger.Initialize();

        // Load all exisiting jobs
        configManager.LoadJobs(jobManager);

        // Load settings and apply language
        var settings = configManager.LoadSettings();
        localization.SetLanguage(settings.Language);
       
        menuHandler.ShowMenu();
    }
}
