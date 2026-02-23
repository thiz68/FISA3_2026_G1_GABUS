namespace EasySave.Core.Services;

using System.Diagnostics;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;

//This class handles operations of copying for backups
//It supports full and differential backups
public class FileBackupService
{
    //Process CryptoSoft
    private readonly CryptoSoftRunner _cryptoRunner = new();

    // Count priority-eligible files for pre-registration in the PriorityGate
    public int CountPriorityFiles(string sourceDir, string targetDir, string jobType, string priorityExtension)
    {
        if (string.IsNullOrEmpty(priorityExtension)) return 0;
        string[] allFiles;
        try { allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories); }
        catch (IOException) { return 0; }

        int count = 0;
        foreach (var file in allFiles)
        {
            if (Path.GetExtension(file).ToLowerInvariant() != priorityExtension) continue;
            if (jobType == "diff")
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                var tf = Path.Combine(targetDir, rel);
                if (File.Exists(tf) && File.GetLastWriteTime(tf) >= File.GetLastWriteTime(file)) continue;
            }
            count++;
        }
        return count;
    }

    // Copy an entire directory using a two-phase approach:
    //   Phase 1 – copy priority files only; call gate.Done() after each (copy+encrypt+log chain).
    //   Phase 2 – wait for the global priority phase to end, then copy the rest.
    // Returns true if the backup fully succeeded, false otherwise.
    // shouldPause: when non-null and returns true, the current file is deferred until it returns false
    //              (business-software pause). Distinct from shouldStop (hard abort by the user).
    public bool CopyDirectory(string sourceDir, string targetDir, IJob job, ILogger logger,
        IStateManager stateManager, ILocalizationService localization,
        Func<bool>? shouldStop = null, PriorityGate? gate = null,
        string priorityExtension = "", LargeFileGate? largeFileGate = null,
        Func<bool>? shouldPause = null)
    {
        // Collect all files that need to be copied (diff filter applied)
        List<(string src, string tgt)> eligible;
        try { eligible = CollectEligibleFiles(sourceDir, targetDir, job.Type); }
        catch (IOException) { return false; }

        try { Directory.CreateDirectory(targetDir); }
        catch (IOException) { return false; }

        int totalFiles = eligible.Count;
        long totalSize = eligible.Sum(f => new FileInfo(f.src).Length);
        int filesRemaining = totalFiles;
        long sizeRemaining = totalSize;

        bool hasPriority = !string.IsNullOrEmpty(priorityExtension);
        var priorityFiles = hasPriority
            ? eligible.Where(f => Path.GetExtension(f.src).ToLowerInvariant() == priorityExtension).ToList()
            : new List<(string src, string tgt)>();
        var nonPriorityFiles = hasPriority
            ? eligible.Where(f => Path.GetExtension(f.src).ToLowerInvariant() != priorityExtension).ToList()
            : eligible;

        bool success = true;

        // ── Phase 1: priority files ──────────────────────────────────────────────
        // We MUST call gate.Done() for every slot we entered (even on abort) to avoid
        // deadlocking jobs that are blocked at WaitIfBlocked.
        int priorityProcessed = 0;
        foreach (var (src, tgt) in priorityFiles)
        {
            bool acquired = false;
            try
            {
                if (shouldStop?.Invoke() == true) { success = false; break; }
                WaitWhilePaused(shouldStop, shouldPause);           // pause until business software stops
                if (shouldStop?.Invoke() == true) { success = false; break; }
                try { Directory.CreateDirectory(Path.GetDirectoryName(tgt)!); }
                catch (IOException) { success = false; break; }

                if (largeFileGate != null && new FileInfo(src).Length > largeFileGate.ThresholdKB * 1024L)
                {
                    acquired = largeFileGate.Acquire(shouldStop);
                    if (!acquired) { success = false; break; }
                }

                var prog = totalFiles > 0 ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
                UpdateStateForFile(job, src, tgt, filesRemaining, sizeRemaining, prog, stateManager, localization);

                long fileSize = CopyFile(src, tgt, logger, job);
                filesRemaining--;
                sizeRemaining -= fileSize;

                prog = totalFiles > 0 ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
                UpdateStateForFile(job, src, tgt, filesRemaining, sizeRemaining, prog, stateManager, localization);
            }
            finally
            {
                // Always release the large-file slot and signal the priority gate,
                // including when we break early (finally still runs on break in C#).
                if (acquired) largeFileGate!.Release();
                gate?.Done();
                priorityProcessed++;
            }
            if (!success) break;
        }

        // Drain any priority slots we never entered (early abort) so other jobs
        // waiting at WaitIfBlocked are not deadlocked.
        if (gate != null)
            for (int i = priorityProcessed; i < priorityFiles.Count; i++)
                gate.Done();

        // ── Wait for the global priority phase to finish ─────────────────────────
        gate?.WaitIfBlocked(shouldStop);
        if (shouldStop?.Invoke() == true) return false;
        WaitWhilePaused(shouldStop, shouldPause);           // business software may still be active
        if (shouldStop?.Invoke() == true) return false;

        // ── Phase 2: non-priority files ──────────────────────────────────────────
        if (success)
        {
            foreach (var (src, tgt) in nonPriorityFiles)
            {
                if (shouldStop?.Invoke() == true) { success = false; break; }
                WaitWhilePaused(shouldStop, shouldPause);           // pause until business software stops
                if (shouldStop?.Invoke() == true) { success = false; break; }
                try { Directory.CreateDirectory(Path.GetDirectoryName(tgt)!); }
                catch (IOException) { success = false; break; }

                bool acquired = false;
                try
                {
                    if (largeFileGate != null && new FileInfo(src).Length > largeFileGate.ThresholdKB * 1024L)
                    {
                        acquired = largeFileGate.Acquire(shouldStop);
                        if (!acquired) { success = false; break; }
                    }

                    var prog = totalFiles > 0 ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
                    UpdateStateForFile(job, src, tgt, filesRemaining, sizeRemaining, prog, stateManager, localization);

                    long fileSize = CopyFile(src, tgt, logger, job);
                    filesRemaining--;
                    sizeRemaining -= fileSize;

                    prog = totalFiles > 0 ? Math.Round((1 - (double)filesRemaining / totalFiles) * 100, 2) : 0;
                    UpdateStateForFile(job, src, tgt, filesRemaining, sizeRemaining, prog, stateManager, localization);
                }
                finally
                {
                    if (acquired) largeFileGate!.Release();
                }
                if (!success) break;
            }
        }

        return success;
    }

    // Returns all (src, tgt) pairs that must be copied, respecting the diff filter.
    // Throws IOException if the source directory is unavailable.
    private List<(string src, string tgt)> CollectEligibleFiles(string sourceDir, string targetDir, string jobType)
    {
        var result = new List<(string src, string tgt)>();
        var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        foreach (var src in allFiles)
        {
            var rel = Path.GetRelativePath(sourceDir, src);
            var tgt = Path.Combine(targetDir, rel);
            if (jobType == "diff" && File.Exists(tgt) && File.GetLastWriteTime(tgt) >= File.GetLastWriteTime(src))
                continue;
            result.Add((src, tgt));
        }
        return result;
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

    // Block the calling thread while shouldPause returns true (business software running),
    // polling every 500 ms. Returns as soon as the software stops or a hard stop fires.
    private static void WaitWhilePaused(Func<bool>? shouldStop, Func<bool>? shouldPause)
    {
        if (shouldPause == null) return;
        while (shouldPause.Invoke())
        {
            if (shouldStop?.Invoke() == true) return;
            Thread.Sleep(500);
        }
    }

    // Update state manager progress information.
    private void UpdateStateForFile(IJob job, string currentSource, string currentTarget,
        int remainingFiles, long remainingSize, double progression, IStateManager stateManager, ILocalizationService localization)
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
