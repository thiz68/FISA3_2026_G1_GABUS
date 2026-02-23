namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;

// Thread-safe global coordinator for cross-job priority file enforcement.
// Non-priority files block via Monitor.Wait until all priority files (across all jobs)
// have been fully processed (copy + optional encrypt + log).
public sealed class PriorityController
{
    private int _pending;
    private readonly object _gate = new();

    // Normalized extension, e.g. ".exe". Empty string means no priority configured.
    public readonly string Ext;

    public PriorityController(string rawExtension)
    {
        var e = (rawExtension ?? "").Trim().ToLower();
        Ext = e.Length > 0 ? (e[0] == '.' ? e : "." + e) : "";
    }

    public bool IsActive => Ext.Length > 0;

    public void Initialize(int count)
    {
        lock (_gate) _pending = count;
    }

    public bool IsPriority(string filePath) =>
        IsActive && Path.GetExtension(filePath).Equals(Ext, StringComparison.OrdinalIgnoreCase);

    // Non-priority files call this before starting; blocks while any pending priority file exists.
    public void WaitIfNonPriority(string filePath)
    {
        if (!IsActive || IsPriority(filePath)) return;
        lock (_gate)
            while (_pending > 0)
                Monitor.Wait(_gate);
    }

    // Called after a priority file is fully processed (copy + encrypt + log).
    public void NotifyDone()
    {
        if (!IsActive) return;
        lock (_gate)
        {
            if (_pending > 0 && --_pending == 0)
                Monitor.PulseAll(_gate);
        }
    }

    // Called when a job aborts early (shouldStop, I/O error) to release waiting threads
    // for the given number of unprocessed priority files.
    public void ReleaseRemaining(int unprocessedPriorityCount)
    {
        if (!IsActive || unprocessedPriorityCount <= 0) return;
        lock (_gate)
        {
            _pending = Math.Max(0, _pending - unprocessedPriorityCount);
            if (_pending == 0) Monitor.PulseAll(_gate);
        }
    }
}

// Execution of backup jobs
public class BackupExecutor
{
    private readonly FileBackupService _fileBackupService;
    private static ILocalizationService? _localization;

    public BackupExecutor(ILocalizationService localization)
    {
        _fileBackupService = new FileBackupService();
        _localization = localization;
    }

    // Execute jobs sequentially (legacy path – within-job priority ordering still applies
    // via file sorting in FileBackupService; no cross-job blocking needed here).
    public string ExecuteSequential(List<IJob> jobs, ILogger logger, IStateManager stateManager, Func<bool>? shouldStop = null)
    {
        bool allSuccess = true;

        foreach (var job in jobs)
        {
            var state = new JobState { State = _localization!.GetString("active") };
            stateManager.UpdateJobState(job, state);

            bool success = _fileBackupService.CopyDirectory(job.SourcePath, job.TargetPath, job, logger, stateManager, _localization!, shouldStop);

            if (success)
            {
                state.State = _localization!.GetString("completed");
                state.Progression = 100;
            }
            else
            {
                state.State = _localization!.GetString("failed");
                allSuccess = false;
            }

            stateManager.UpdateJobState(job, state);
        }

        return allSuccess ? "backup_completed" : "backup_failed";
    }

    // Execute all jobs in parallel with a shared PriorityController so that no
    // non-priority file in any job is processed while at least one priority file
    // remains pending across the entire batch.
    public string ExecuteParallel(List<IJob> jobs, ILogger logger, IStateManager stateManager, Func<bool>? shouldStop = null)
    {
        var settings = new ConfigManager().LoadSettings();
        var controller = new PriorityController(settings.PriorityExtension ?? "");

        if (controller.IsActive)
        {
            // Count eligible priority files across all jobs (respects diff-backup filter)
            // so the counter is exact and threads are never stuck waiting for a file that
            // will never be processed.
            int total = 0;
            foreach (var job in jobs)
            {
                try
                {
                    var allFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);
                    foreach (var f in allFiles)
                    {
                        if (!controller.IsPriority(f)) continue;
                        if (job.Type == "diff")
                        {
                            var rel = Path.GetRelativePath(job.SourcePath, f);
                            var target = Path.Combine(job.TargetPath, rel);
                            if (File.Exists(target) && File.GetLastWriteTime(target) >= File.GetLastWriteTime(f))
                                continue;
                        }
                        total++;
                    }
                }
                catch (IOException) { }
            }
            controller.Initialize(total);
        }

        // Mark all jobs as active before starting
        foreach (var job in jobs)
        {
            var state = new JobState { State = _localization!.GetString("active") };
            stateManager.UpdateJobState(job, state);
        }

        int failCount = 0;
        Parallel.ForEach(jobs, job =>
        {
            bool success = _fileBackupService.CopyDirectory(
                job.SourcePath, job.TargetPath, job, logger, stateManager, _localization!, shouldStop, controller);

            var endState = new JobState
            {
                State = success ? _localization!.GetString("completed") : _localization!.GetString("failed"),
                Progression = success ? 100 : 0
            };
            stateManager.UpdateJobState(job, endState);

            if (!success)
                Interlocked.Increment(ref failCount);
        });

        return failCount == 0 ? "backup_completed" : "backup_failed";
    }
}
