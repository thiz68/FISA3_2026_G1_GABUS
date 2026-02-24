/*
 * BackupExecutor: orchestrates parallel backup job execution.
 * Concurrency is clamped to [1, MaxConcurrency] slots via SemaphoreSlim.
 * Two shared gates are constructed before any task starts so counters are
 * complete when the first thread enters copy logic:
 *   - PriorityGate:  blocks non-priority threads until ALL priority files
 *                    across ALL jobs have been copied + encrypted + logged.
 *   - LargeFileGate: limits simultaneous large-file (>ThresholdKB) transfers
 *                    to one at a time, shared across all concurrent jobs.
 */
namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;

public class BackupExecutor
{
    private readonly FileBackupService _fileBackupService;
    private static ILocalizationService? _localization;

    // Maximum concurrency based on processor count (exposed for UI display)
    public static int MaxConcurrency => Math.Clamp(Environment.ProcessorCount, 1, 8);

    public BackupExecutor(ILocalizationService localization)
    {
        _fileBackupService = new FileBackupService();
        _localization = localization;
    }

    // Execute jobs in parallel using SemaphoreSlim to limit concurrency.
    // Returns "backup_completed" if all succeeded, "backup_failed" if any failed.
    public string ExecuteSequential(
        List<IJob> jobs,
        ILogger logger,
        IStateManager stateManager,
        Func<string, bool>? shouldStop = null,
        Func<string, bool>? shouldPause = null)
    {
        int maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();

        // Build global priority gate: pre-scan all jobs so the counter is complete before any copy starts.
        var settings1 = new ConfigManager().LoadSettings();
        string priorityExt = NormalizePriorityExtension(settings1.PriorityExtension);
        PriorityGate? gate = null;
        if (!string.IsNullOrEmpty(priorityExt))
        {
            gate = new PriorityGate();
            int total = 0;
            foreach (var j in jobs)
                total += _fileBackupService.CountPriorityFiles(j.SourcePath, j.TargetPath, j.Type, priorityExt);
            gate.Add(total);
        }

        // Build global large-file gate (null = disabled).
        LargeFileGate? largeFileGate = settings1.LargeFileThresholdKB > 0
            ? new LargeFileGate(settings1.LargeFileThresholdKB)
            : null;

        foreach (var job in jobs)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var state = new JobState { State = _localization.GetString("active") };
                    stateManager.UpdateJobState(job, state);

                    bool success = _fileBackupService.CopyDirectory(
                        job.SourcePath,
                        job.TargetPath,
                        job,
                        logger,
                        stateManager,
                        _localization,
                        () => shouldStop?.Invoke(job.Name) ?? false,
                        gate,
                        priorityExt,
                        largeFileGate,
                        () => shouldPause?.Invoke(job.Name) ?? false
                    );

                    if (success)
                    {
                        state.State = _localization.GetString("completed");
                        state.Progression = 100;
                    }
                    else
                    {
                        // Drive unavailable, USB unplugged, hard stop, or business software abort.
                        state.State = _localization.GetString("failed");
                    }
                    stateManager.UpdateJobState(job, state);
                    return success;
                }

                finally
                {
                    semaphore.Release();
                }
            }));
        }

        Task.WhenAll(tasks).Wait();
        bool allSuccess = tasks.All(t => t.Result);
        return allSuccess ? "backup_completed" : "backup_failed";
    }

    // Execute jobs in parallel with UI progress callbacks.
    // progressCallback:    (jobName, progressPercent, isFailed)
    // shouldStop:          per-job hard-stop (definitive; no recovery).
    // shouldPause:         per-job soft pause (business software or manual; resumes automatically).
    // onPauseStateChanged: optional notification on pause-state transitions.
    public void ExecuteWithProgress(
        List<IJob> jobs,
        ILogger logger,
        IStateManager stateManager,
        Action<string, double, bool> progressCallback,
        Action<bool> completionCallback,
        Func<string, bool>? shouldStop = null,
        Func<string, bool>? shouldPause = null,
        Action<bool>? onPauseStateChanged = null)
    {
        int maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();

        // Build global priority gate (same logic as ExecuteSequential).
        var settings2 = new ConfigManager().LoadSettings();
        string priorityExt = NormalizePriorityExtension(settings2.PriorityExtension);
        PriorityGate? gate = null;
        if (!string.IsNullOrEmpty(priorityExt))
        {
            gate = new PriorityGate();
            int total = 0;
            foreach (var j in jobs)
                total += _fileBackupService.CountPriorityFiles(j.SourcePath, j.TargetPath, j.Type, priorityExt);
            gate.Add(total);
        }

        // Build global large-file gate (null = disabled).
        LargeFileGate? largeFileGate = settings2.LargeFileThresholdKB > 0
            ? new LargeFileGate(settings2.LargeFileThresholdKB)
            : null;

        // Wrap stateManager to intercept per-file state updates and forward progress to the UI callback.
        var progressStateManager = new ProgressTrackingStateManager(stateManager, progressCallback);

        foreach (var job in jobs)
        {
            progressCallback(job.Name, 0, false);

            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var state = new JobState { State = _localization.GetString("active") };
                    progressStateManager.UpdateJobState(job, state);

                    bool success = _fileBackupService.CopyDirectory(
                        job.SourcePath,
                        job.TargetPath,
                        job,
                        logger,
                        progressStateManager,
                        _localization,
                        () => shouldStop?.Invoke(job.Name) ?? false,
                        gate,
                        priorityExt,
                        largeFileGate,
                        () => shouldPause?.Invoke(job.Name) ?? false
                    );

                    if (success)
                    {
                        state.State = _localization.GetString("completed");
                        state.Progression = 100;
                        progressCallback(job.Name, 100, false);
                    }
                    else
                    {
                        // Drive unavailable, USB unplugged, hard stop, or business software abort.
                        state.State = _localization.GetString("failed");
                        progressCallback(job.Name, -1, true);
                    }
                    progressStateManager.UpdateJobState(job, state);
                    return success;
                }

                finally
                {
                    semaphore.Release();
                }
            }));
        }

        Task.WhenAll(tasks).ContinueWith(t =>
        {
            bool allSuccess = tasks.All(task => task.Result);
            completionCallback(allSuccess);
        });
    }

    // Normalize priority extension: "exe" or ".EXE" → ".exe"; null/empty → ""
    private static string NormalizePriorityExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return string.Empty;
        ext = ext.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : '.' + ext;
    }
}

// Global priority gate: blocks non-priority file copies until all priority files across all jobs are done.
public sealed class PriorityGate
{
    private int _pending;
    private readonly object _sync = new();

    // Add count of priority files before any copy starts (called from BackupExecutor, single-threaded).
    public void Add(int count)
    {
        lock (_sync) { _pending += count; }
    }

    // Signal that one priority file has been fully processed (copy + encrypt + log).
    public void Done()
    {
        lock (_sync)
        {
            if (--_pending <= 0)
            {
                _pending = 0;
                Monitor.PulseAll(_sync); // wake all waiting non-priority threads
            }
        }
    }

    // Block the calling thread until _pending == 0; checks shouldStop every 200 ms.
    // While shouldPause is active, keeps waiting without returning so the job stays alive.
    public void WaitIfBlocked(Func<bool>? shouldStop, Func<bool>? shouldPause = null)
    {
        lock (_sync)
        {
            while (_pending > 0)
            {
                // Pause active: don't check stop, just keep waiting.
                if (shouldPause?.Invoke() == true) { Monitor.Wait(_sync, 200); continue; }
                if (shouldStop?.Invoke() == true) return;
                Monitor.Wait(_sync, 200);
            }
        }
    }
}

// Global large-file gate: at most one transfer of a file > ThresholdKB is allowed at a time.
// Files <= ThresholdKB are never blocked by this gate.
// Scope: shared across all concurrent jobs (created once in BackupExecutor).
public sealed class LargeFileGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public long ThresholdKB { get; }

    public LargeFileGate(long thresholdKb) => ThresholdKB = thresholdKb;

    // Acquires the slot. Returns true if acquired, false if shouldStop fired.
    // While shouldPause is active, yields without holding the semaphore so
    // other non-paused jobs can use the bandwidth slot; resumes when pause clears.
    public bool Acquire(Func<bool>? shouldStop, Func<bool>? shouldPause = null)
    {
        while (true)
        {
            if (shouldStop?.Invoke() == true) return false;
            // Paused: don't try to grab the slot, just wait and retry.
            if (shouldPause?.Invoke() == true) { Thread.Sleep(200); continue; }
            if (_semaphore.Wait(200)) return true;
        }
    }

    // Always call from a finally block so the slot is never leaked on error.
    public void Release() => _semaphore.Release();
}

// Decorator: intercepts IStateManager.UpdateJobState calls to forward progress percentages to the UI.
internal class ProgressTrackingStateManager : IStateManager
{
    private readonly IStateManager _innerStateManager;
    private readonly Action<string, double, bool> _progressCallback;

    public ProgressTrackingStateManager(IStateManager innerStateManager, Action<string, double, bool> progressCallback)
    {
        _innerStateManager = innerStateManager;
        _progressCallback = progressCallback;
    }

    public void UpdateJobState(IJob job, JobState state)
    {
        _innerStateManager.UpdateJobState(job, state);
        _progressCallback(job.Name, state.Progression, false);
    }

    public void SaveState()
    {
        _innerStateManager.SaveState();
    }
}
