namespace EasySave.Core.Services;

using System.Diagnostics;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;

// Handles copy operations for backup jobs (full and differential).
// When a PriorityController is supplied, non-priority files are blocked until all
// priority files (globally across all parallel jobs) have been fully processed.
public class FileBackupService
{
    private readonly CryptoSoftRunner _cryptoRunner = new();

    // Copy an entire directory tree from sourceDir to targetDir.
    // Files are globally sorted (priority-first) before processing so that within-job
    // ordering is correct even in sequential mode.
    // Returns true on success, false on any failure (I/O error, business software, etc.).
    public bool CopyDirectory(string sourceDir, string targetDir, IJob job, ILogger logger,
        IStateManager stateManager, ILocalizationService localization,
        Func<bool>? shouldStop = null, PriorityController? priority = null)
    {
        // Resolve the active priority extension:
        //   - from the shared controller when running in parallel mode
        //   - from settings directly when running in sequential mode (no controller)
        string priorityExt;
        if (priority?.IsActive == true)
        {
            priorityExt = priority.Ext;
        }
        else
        {
            var raw = (new ConfigManager().LoadSettings().PriorityExtension ?? "").Trim().ToLower();
            priorityExt = raw.Length > 0 && raw[0] != '.' ? "." + raw : raw;
        }

        bool isPrio(string path) =>
            priorityExt.Length > 0 &&
            Path.GetExtension(path).Equals(priorityExt, StringComparison.OrdinalIgnoreCase);

        // Collect all eligible files upfront (honours diff-backup filter)
        var eligibleFiles = GetEligibleFiles(sourceDir, targetDir, job.Type);
        int totalFiles = eligibleFiles.Count;
        long totalSize = 0;
        foreach (var f in eligibleFiles)
        {
            try { totalSize += new FileInfo(f).Length; } catch { }
        }

        if (totalFiles == 0 && totalSize == 0)
        {
            try
            {
                if (!Directory.Exists(sourceDir)) return false;
            }
            catch (IOException) { return false; }
        }

        try { Directory.CreateDirectory(targetDir); }
        catch (IOException)
        {
            // Release all expected priority-file slots so parallel jobs are not deadlocked
            priority?.ReleaseRemaining(eligibleFiles.Count(f => isPrio(f)));
            return false;
        }

        // Global sort: priority files first so ordering is correct on a single thread too
        if (priorityExt.Length > 0)
            eligibleFiles = eligibleFiles.OrderBy(f => isPrio(f) ? 0 : 1).ToList();

        // Track how many priority files we still owe notifications for (used on early exit)
        int priorityOwed = eligibleFiles.Count(f => isPrio(f));

        int filesRemaining = totalFiles;
        long sizeRemaining = totalSize;

        for (int i = 0; i < eligibleFiles.Count; i++)
        {
            var sourceFile = eligibleFiles[i];
            var rel = Path.GetRelativePath(sourceDir, sourceFile);
            var targetFile = Path.Combine(targetDir, rel);

            try { Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!); }
            catch (IOException)
            {
                // Ensure every unprocessed priority slot is released from this index on
                priority?.ReleaseRemaining(priorityOwed);
                return false;
            }

            // Non-priority file: block until all globally-pending priority files are done
            priority?.WaitIfNonPriority(sourceFile);

            var progression = totalFiles > 0
                ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
            UpdateStateForFile(job, sourceFile, targetFile, filesRemaining, sizeRemaining,
                progression, stateManager, localization);

            long fileSize = CopyFile(sourceFile, targetFile, logger, job);

            // Priority file fully done (copy + optional encrypt + log written inside CopyFile)
            if (isPrio(sourceFile))
            {
                priority?.NotifyDone();
                priorityOwed--;
            }

            filesRemaining--;
            sizeRemaining -= fileSize;

            progression = totalFiles > 0
                ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
            UpdateStateForFile(job, sourceFile, targetFile, filesRemaining, sizeRemaining,
                progression, stateManager, localization);

            if (shouldStop?.Invoke() == true)
            {
                // Release any remaining priority slots (files i+1 … end) so waiting threads
                // in other parallel jobs are not stuck indefinitely
                priority?.ReleaseRemaining(priorityOwed);
                var settings = new ConfigManager().LoadSettings();
                logger.LogBusinessSoftwareStop(DateTime.Now, job.Name, settings.BusinessSoftware);
                return false;
            }
        }

        return true;
    }

    // Returns the list of files that need to be copied (all for full, only changed for diff)
    private List<string> GetEligibleFiles(string sourceDir, string targetDir, string type)
    {
        string[] allFiles;
        try
        {
            allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        }
        catch (IOException) { return []; }

        var result = new List<string>();
        foreach (var file in allFiles)
        {
            if (type == "diff")
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                var target = Path.Combine(targetDir, rel);
                if (File.Exists(target) && File.GetLastWriteTime(target) >= File.GetLastWriteTime(file))
                    continue;
            }
            result.Add(file);
        }
        return result;
    }

    // Copy a single file, optionally encrypt it, then write the transfer log entry.
    // Returns the file size (always; never throws – errors are logged with negative time).
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

    private void UpdateStateForFile(IJob job, string currentSource, string currentTarget,
        int remainingFiles, long remainingSize, double progression,
        IStateManager stateManager, ILocalizationService localization)
    {
        var state = new JobState
        {
            State = localization.GetString("active"),
            NbFilesLeftToDo = remainingFiles,
            NbSizeLeftToDo = remainingSize,
            Progression = progression,
            CurrentSourceFilePath = currentSource,
            CurrentTargetFilePath = currentTarget
        };

        stateManager.UpdateJobState(job, state);
    }
}
