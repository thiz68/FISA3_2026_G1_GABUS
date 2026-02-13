namespace EasySave.Core.Services;

using System.IO;
using System.Reflection;

public class PathValidator
{
    // Checks if the source path is valid: exists and does not contain the executable
    public bool IsSourceValid(string? sourcePath)
    {
        // Check if source exist
        if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
        {
            return false;
        }

        // Check if source is executable folder
        string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        string fullSource = Path.GetFullPath(sourcePath);
        string fullExe = Path.GetFullPath(exePath);
        bool isInside = fullExe.Equals(fullSource, StringComparison.OrdinalIgnoreCase) ||
        fullExe.StartsWith(fullSource + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        return !isInside;
    }

    // Checks if the target path is valid: can create a directory within it
    public bool IsTargetValid(string? targetPath)
    {
        // Check if target is absolute
        if (string.IsNullOrWhiteSpace(targetPath) || !Path.IsPathRooted(targetPath))
        {
            return false;
        }

        // Check if directory can be created in target
        string testDir = Path.Combine(targetPath, Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(testDir);
            Directory.Delete(testDir);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
