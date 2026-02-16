namespace EasySave.Core.Services;

using System.Diagnostics;

public class BusinessSoftwareChecker
{
    public bool IsBusinessSoftwareRunning(string processNames)
    {
        if (string.IsNullOrWhiteSpace(processNames))
            return false;

        // Support multiple processes separated by ;
        var names = processNames.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var processName in names)
        {
            var name = processName.Trim().Replace(".exe", "", StringComparison.OrdinalIgnoreCase);

            if (Process.GetProcessesByName(name).Length > 0)
                return true;
        }

        return false;
    }
}
