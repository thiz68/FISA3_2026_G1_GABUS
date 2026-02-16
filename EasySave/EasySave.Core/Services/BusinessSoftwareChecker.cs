namespace EasySave.Core.Services;

using System.Diagnostics;

public class BusinessSoftwareChecker
{
    public bool IsBusinessSoftwareRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        var name = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);

        return Process.GetProcessesByName(name).Length > 0;
    }
}
