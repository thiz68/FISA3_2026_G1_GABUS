namespace EasySave.Core.Services;

using System.Diagnostics;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;

//This class heandles operations of copying for backups
//It supports full and differential backups
public class FileBackupService
{
    // Translation service
    private static ILocalizationService _localization = null!;

    //Copy an entire dir from source to target
    //Returns true if backup succeeded, false if it failed (drive unavailable, etc.)
    public bool CopyDirectory(string sourceDir, string targetDir, IJob job, ILogger logger, IStateManager stateManager)
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
            //Is the target folder existing ?
            Directory.CreateDirectory(targetDir);
        }
        catch (IOException)
        {
            // Cannot create target directory, abort backup
            return false;
        }

        //Copy progress
        bool success = true;
        CopyDirectoryRecursive(sourceDir, targetDir, job, logger, stateManager, totalFiles, totalSize, ref filesRemaining, ref sizeRemaining, ref success);
        return success;
    }
    
    //Copy all files and subfolders /!\ RECURSIVE /!\
    private void CopyDirectoryRecursive(string sourceDir, string targetDir, IJob job, ILogger logger,
        IStateManager stateManager, int totalFiles, long totalSize, ref int filesRemaining, ref long sizeRemaining, ref bool success)
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
            var fileName = Path.GetFileName(sourceFile);
            var targetFile = Path.Combine(targetDir, fileName);

            //DIFFERENTIAL : skip files no changes, compare "last modified" dates to decide.
            if (job.Type == "diff" && File.Exists(targetFile))
            {
                if (File.GetLastWriteTime(targetFile) >= File.GetLastWriteTime(sourceFile))
                    continue;
            }

            var fileSize = CopyFile(sourceFile, targetFile, job, logger, stateManager);

            //update counters
            filesRemaining--;
            sizeRemaining -= fileSize;

            //Calculate % complete, and preventing division / 0
            double progression = totalSize > 0 ? ((totalSize - sizeRemaining) * 100.0 / totalSize) : 100;

            //Tell state manager about the progess
            UpdateStateForFile(job, sourceFile, targetFile, filesRemaining, sizeRemaining, progression, stateManager);
        }

        // Get list of subdirectories, handle errors if drive becomes unavailable
        string[] subDirs;
        try
        {
            subDirs = Directory.GetDirectories(sourceDir);
        }
        catch (IOException)
        {
            // Drive unavailable, mark as failed and stop
            success = false;
            return;
        }

        //Process all subfolders
        foreach (var sourceSubDir in subDirs)
        {
            var dirName = Path.GetFileName(sourceSubDir);
            var targetSubDir = Path.Combine(targetDir, dirName);

            // Try to create subfolder, skip if drive becomes unavailable
            try
            {
                Directory.CreateDirectory(targetSubDir);
            }
            catch (IOException)
            {
                // Cannot create subdirectory, mark as failed and skip
                success = false;
                continue;
            }

            CopyDirectoryRecursive(sourceSubDir, targetSubDir, job, logger, stateManager,
                totalFiles, totalSize, ref filesRemaining, ref sizeRemaining, ref success);
        }
    }
    
    //Copy single file and log, return file size
    private long CopyFile(string sourceFile, string targetFile, IJob job, ILogger logger, IStateManager stateManager)
    {
        var fileInfo = new FileInfo(sourceFile);
        var fileSize = fileInfo.Length;
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            //Copy the file. "overwrite: true" means replace if it exists.
            File.Copy(sourceFile, targetFile, overwrite: true);
            stopwatch.Stop();
            
            //Log successful
            //Something went wrong
            logger.LogFileTransfer(DateTime.Now, job.Name, sourceFile, targetFile, fileSize, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            //Problem
            stopwatch.Stop();
            
            //Log failed
            logger.LogFileTransfer(DateTime.Now, job.Name, sourceFile, targetFile, fileSize, -stopwatch.ElapsedMilliseconds);
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

        //Recursive Search
        foreach (var file in allFiles)
        {
            var fileInfo = new FileInfo(file);

            //DIFFERENTIAL : check file needed backup
            if (type == "diff")
            {
                //Convert absolute path to relative path
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var targetFile = Path.Combine(targetDir, relativePath);

                //Skip if target exists and is up-to-date
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
        int remainingFiles, long remainingSize, double progression, IStateManager stateManager)
    {
        _localization = new LocalizationService();
        // Create state object with all information.
        var state = new JobState
        {
            State = _localization.GetString("active"),
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