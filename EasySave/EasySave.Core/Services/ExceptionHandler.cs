namespace EasySave.ConsoleApp;

using EasySave.Core.Interfaces;

public static class ExceptionHandler
{
    private static ILocalizationService? _localization;
    public static void SetLocalization(ILocalizationService localization)
    {
        _localization = localization;
    }
    
    // Handles unexpected errors that are not caught elsewhere
    // This prevents the app from crashing when USB drives are plugged/unplugged
    public static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        // Display error message to user
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(_localization?.GetString("critical_error") ?? "A critical error occurred");
        
        // Show exception details for debugging
        if (exception != null)
        {
            Console.WriteLine($"Error: {exception.Message}");
        }
        Console.WriteLine("========================================");
        Console.WriteLine(_localization?.GetString("press_to_continue") ?? "Press any key to continue...");
        Console.ReadKey();
    }
}