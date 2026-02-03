namespace EasySave.Core.Services;

using System.Diagnostics;
using EasySave.Core.Enums;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;

//This class heandles operations of copying for backups
//It supports full and differential backups
public class FileBackupService
{
    //Copy an entire dir from source to target
    public void CopyDirectory(string sourceDir, string targetDir, IJob job, ILogger logger, IStateManager stateManager)
    {
        //Counting how many files need to copy and total size
        var (totalFiles, totalSize) = CalculateEligibleFiles(sourceDir, targetDir, job.Type);
        
        //counters
        int filesRemaining = totalFiles;
        long sizeRemaining = totalSize;
        
        //Is the target folder existing ?
        Directory.CreateDirectory(targetDir);
        
        //Copy progress
        CopyDirectoryRecursive(sourceDir, targetDir, job, logger, stateManager, totalFiles, totalSize, ref filesRemaining, ref sizeRemaining);
    }
    
    //Copy all files and subfolders /!\ RECURSIVE /!\
    private void CopyDirectoryRecursive(string sourceDir, string targetDir, IJob job, ILogger logger,
        IStateManager stateManager, int totalFiles, long totalSize, ref int FilesRemaining, ref long SizeRemaining)
    {
        //Copy all files in the current folder
        foreach (var sourceFile in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(sourceFile);
            var targetFile = Path.Combine(targetDir, fileName);
            
            //DIFFERENTIAL : skip files no changes, compare "last modified" dates to decide.
            if (job.Type == SaveType.Differential && File.Exists(targetFile))
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
        
        //Process all subfolders
        foreach (var sourceSubDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(sourceSubDir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            
            Directory.CreateDirectory(targetSubDir);
            
            CopyDirectoryRecursive(sourceSubDir, targetSubDir, job, logger, stateManager,
                totalFiles, totalSize, ref filesRemaining, ref sizeRemaining);
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

    private (int fileCount, long totalSize) CalculateEligibleFiles(string sourceDir, string targetDir, SaveType type)
    {
        int count = 0;
        long size = 0;
        
        //Recursive Search
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var fileInfo = new FileInfo(file);
            
            //DIFFERENTIAL : check file needed backup
            if (type == SaveType.Differential)
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
        // Create state object with all information.
        var state = new JobState
        {
            State = "Active",
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