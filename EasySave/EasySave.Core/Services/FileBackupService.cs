namespace EasySave.Core.Services;

using System.Diagnostics;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;

//This class heandles operations of copying for backups
//It supports full and differential backups
public class FileBackupService
{
    //Process CryptoSoft
    private readonly CryptoSoftRunner _cryptoRunner = new();
    
    //Copy an entire dir from source to target
    //Returns true if backup succeeded, false if it failed (drive unavailable, etc.)
    public bool CopyDirectory(string sourceDir, string targetDir, IJob job, ILogger logger, IStateManager stateManager, ILocalizationService localization)
    {
        //Counting how many files need to copy and total size
        var (totalFiles, totalSize) = CalculateEligibleFiles(sourceDir, targetDir, job.Type);

        // If no files found, drive might be unavailable
        if (totalFiles == 0 && totalSize == 0)
        {
            // Check if source directory is actually empty or if there was an error
            try
            {
                if (!Directory.Exists(sourceDir))
                    return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        //counters
        int filesRemaining = totalFiles;
        long sizeRemaining = totalSize;

        // Try to create target folder, handle errors if drive is unavailable
        try
        {
            Directory.CreateDirectory(targetDir);
        }
        catch (IOException)
        {
            // Cannot create target directory, abort backup
            return false;
        }

        //Copy progress
        bool success = true;
        CopyDirectoryRecursive(sourceDir, targetDir, job, logger, stateManager, totalFiles, totalSize, ref filesRemaining, ref sizeRemaining, ref success, localization);
        return success;
    }

    //Copy all files and subfolders
    private void CopyDirectoryRecursive(string sourceDir, string targetDir, IJob job, ILogger logger,
    IStateManager stateManager, int totalFiles, long totalSize, ref int filesRemaining, ref long sizeRemaining, ref bool success, ILocalizationService localization)
    {
        // Get list of files, handle errors if drive becomes unavailable (USB unplugged)
        string[] files;
        try
        {
            files = Directory.GetFiles(sourceDir);
        }
        catch (IOException)
        {
            // Drive unavailable, mark as failed and stop
            success = false;
            return;
        }

        //Copy all files in the current folder
        foreach (var sourceFile in files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var targetFile = Path.Combine(targetDir, relativePath);
            // Check if file needs to be copied (for differential backup)
            if (job.Type == "diff")
            {
                if (File.Exists(targetFile) && File.GetLastWriteTime(targetFile) >= File.GetLastWriteTime(sourceFile))
                    continue;
            }

            // Update state before copying
            var progression = totalFiles > 0 ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
            UpdateStateForFile(job, sourceFile, targetFile, filesRemaining, sizeRemaining, progression, stateManager, localization);

            // Copy the file and log
            long fileSize = CopyFile(sourceFile, targetFile, logger, job);

            // Update counters
            filesRemaining--;
            sizeRemaining -= fileSize;

            // Update progression after copy
            progression = totalFiles > 0 ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
            UpdateStateForFile(job, sourceFile, targetFile, filesRemaining, sizeRemaining, progression, stateManager, localization);
        }

        // Get subdirectories, handle errors
        string[] subDirs;
        try
        {
            subDirs = Directory.GetDirectories(sourceDir);
        }
        catch (IOException)
        {
            success = false;
            return;
        }

        // Recurse into subdirectories
        foreach (var subDir in subDirs)
        {
            var relativePath = Path.GetRelativePath(sourceDir, subDir);
            var targetSubDir = Path.Combine(targetDir, relativePath);
            try
            {
                Directory.CreateDirectory(targetSubDir);
            }
            catch (IOException)
            {
                success = false;
                return;
            }
            CopyDirectoryRecursive(subDir, targetSubDir, job, logger, stateManager, totalFiles, totalSize, ref filesRemaining, ref sizeRemaining, ref success, localization);
            if (!success) return;
        }
    }

    //Copy a single file from source to target
    //Returns the size of the file copied
    private long CopyFile(string sourceFile, string targetFile, ILogger logger, IJob job)
    {
        var fileInfo = new FileInfo(sourceFile);
        long fileSize = fileInfo.Length;
        var stopwatch = Stopwatch.StartNew();

        long encryptionTime = 0;

        try
        {
            File.Copy(sourceFile, targetFile, overwrite: true);
            stopwatch.Stop();

            var settings = new ConfigManager().LoadSettings();

            if (!string.IsNullOrWhiteSpace(settings.ExtensionsToEncrypt))
            {
                var extensions = settings.ExtensionsToEncrypt
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLower())
                    .ToList();

                var fileExtension = Path.GetExtension(targetFile).ToLower();

                if (extensions.Contains(fileExtension))
                {
                    if (_cryptoRunner.IsCryptoSoftAvailable())
                    {
                        encryptionTime = _cryptoRunner.EncryptFile(targetFile);
                    }
                    else
                    {
                        encryptionTime = -1;
                    }
                }
            }

            logger.LogFileTransfer(
                DateTime.Now,
                job.Name,
                sourceFile,
                targetFile,
                fileSize,
                stopwatch.ElapsedMilliseconds,
                encryptionTime
            );
        }
        catch (Exception)
        {
            stopwatch.Stop();

            logger.LogFileTransfer(
                DateTime.Now,
                job.Name,
                sourceFile,
                targetFile,
                fileSize,
                -stopwatch.ElapsedMilliseconds,
                -1
            );
        }

        return fileSize;
    }


    private (int fileCount, long totalSize) CalculateEligibleFiles(string sourceDir, string targetDir, string type)
    {
        int count = 0;
        long size = 0;

        // Get all files recursively, handle errors if drive becomes unavailable
        string[] allFiles;
        try
        {
            allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            // Drive unavailable, return zero files
            return (0, 0);
        }

        foreach (var file in allFiles)
        {
            var fileInfo = new FileInfo(file);
            if (type == "diff")
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var targetFile = Path.Combine(targetDir, relativePath);
                if (File.Exists(targetFile) && File.GetLastWriteTime(targetFile) >= File.GetLastWriteTime(file))
                    continue;
            }

            count++;
            size += fileInfo.Length;
        }
        return (count, size);
    }

    // Update  state manager progress information.
    private void UpdateStateForFile(IJob job, string currentSource, string currentTarget,
    int remainingFiles, long remainingSize, double progression, IStateManager stateManager, ILocalizationService localization)
    {
        // Create state object with all information.
        var state = new JobState
        {
            State = localization.GetString("active"),
            NbFilesLeftToDo = remainingFiles,
            NbSizeLeftToDo = remainingSize,
            Progression = progression,
            CurrentSourceFilePath = currentSource,
            CurrentTargetFilePath = currentTarget
        };

        //Update state manager
        stateManager.UpdateJobState(job, state);
    }
}
