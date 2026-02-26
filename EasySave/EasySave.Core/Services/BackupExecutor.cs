namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;

// Execution of backup jobs
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

    // Execute jobs in parallel using SemaphoreSlim to limit concurrency based on processor count
    // Returns "backup_completed" if all succeeded, "backup_failed" if any failed
    public string ExecuteSequential(
        List<IJob> jobs,
        ILogger logger,
        IStateManager stateManager,
        Func<string, bool>? shouldStop = null,
        Func<string, bool>? shouldPause = null)
    {
        // Determine maximum concurrency: clamp between 1 and 8 based on logical processors
        int maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();

        // Build global priority gate: pre-scan all jobs so the counter is complete before any copy starts
        var settings1 = new ConfigManager().LoadSettings();
        var priorityExts1 = ParsePriorityExtensions(settings1.PriorityExtension);
        PriorityGate? gate = null;
        if (priorityExts1.Count > 0)
        {
            gate = new PriorityGate();
            int total = 0;
            foreach (var j in jobs)
                total += _fileBackupService.CountPriorityFiles(j.SourcePath, j.TargetPath, j.Type, priorityExts1);
            gate.Add(total);
        }

        // Build global large-file gate (null = disabled)
        LargeFileGate? largeFileGate = settings1.LargeFileThresholdKB > 0
            ? new LargeFileGate(settings1.LargeFileThresholdKB)
            : null;

        foreach (var job in jobs)
        {
            // Create a task for each job
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // Initialize the state as Active
                    var state = new JobState { State = _localization.GetString("active") };
                    stateManager.UpdateJobState(job, state);

                    // Copy all files from source to target
                    bool success = _fileBackupService.CopyDirectory(
                        job.SourcePath,
                        job.TargetPath,
                        job,
                        logger,
                        stateManager,
                        _localization,
                        () => shouldStop?.Invoke(job.Name) ?? false,
                        gate,
                        priorityExts1,
                        largeFileGate,
                        () => shouldPause?.Invoke(job.Name) ?? false
                    );

                    if (success)
                    {
                        // Mark job as completed
                        state.State = _localization.GetString("completed");
                        state.Progression = 100;
                    }
                    else
                    {
                        // Mark job as failed (drive unavailable, USB unplugged, business software detected, etc.)
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

        // Wait for all tasks to complete synchronously
        Task.WhenAll(tasks).Wait();

        // Check if all jobs succeeded
        bool allSuccess = tasks.All(t => t.Result);
        return allSuccess ? "backup_completed" : "backup_failed";
    }

    // Execute jobs in parallel with callback for view updates
    // progressCallback: (jobName, progressPercent, isFailed)
    // shouldStop: emergency stop callback (definitive stop)
    // shouldPause: business software callback (temporary pause)
    // onPauseStateChanged: callback when pause state changes (true = entering pause, false = exiting)
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
        // Determine maximum concurrency (max threads available on computer)
        int maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();

        // Build global priority gate (same logic as ExecuteSequential)
        var settings2 = new ConfigManager().LoadSettings();
        var priorityExts2 = ParsePriorityExtensions(settings2.PriorityExtension);
        PriorityGate? gate = null;
        if (priorityExts2.Count > 0)
        {
            gate = new PriorityGate();
            int total = 0;
            foreach (var j in jobs)
                total += _fileBackupService.CountPriorityFiles(j.SourcePath, j.TargetPath, j.Type, priorityExts2);
            gate.Add(total);
        }

        // Build global large-file gate (null = disabled)
        LargeFileGate? largeFileGate = settings2.LargeFileThresholdKB > 0
            ? new LargeFileGate(settings2.LargeFileThresholdKB)
            : null;

        // Get progress in job
        var progressStateManager = new ProgressTrackingStateManager(stateManager, progressCallback);

        foreach (var job in jobs)
        {
            // Initialize progress to 0 at start
            progressCallback(job.Name, 0, false);

            // Create a task for each job
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // Initialize the state as Active
                    var state = new JobState { State = _localization.GetString("active") };
                    progressStateManager.UpdateJobState(job, state);

                    // Copy all files from source to target
                    bool success = _fileBackupService.CopyDirectory(
                        job.SourcePath,
                        job.TargetPath,
                        job,
                        logger,
                        progressStateManager,
                        _localization,
                        () => shouldStop?.Invoke(job.Name) ?? false,
                        gate,
                        priorityExts2,
                        largeFileGate,
                        () => shouldPause?.Invoke(job.Name) ?? false
                    );

                    if (success)
                    {
                        // Mark job as completed
                        state.State = _localization.GetString("completed");
                        state.Progression = 100;
                        progressCallback(job.Name, 100, false);
                    }
                    else
                    {
                        // Mark job as failed (drive unavailable, USB unplugged, business software detected, etc.)
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

        // Wait for all tasks and call completion callback
        Task.WhenAll(tasks).ContinueWith(t =>
        {
            bool allSuccess = tasks.All(task => task.Result);
            completionCallback(allSuccess);
        });
    }

    // Parse a ';'-separated priority extension list into an ordered, de-duplicated List<string>.
    // Each token must already be dot-prefixed (e.g. ".exe"); tokens without a leading dot are silently skipped
    // because the UI validation prevents saving them. Tokens are lowercased and de-duplicated (first occurrence wins).
    // Returns an empty list when the input is null/empty/whitespace (priority disabled).
    private static List<string> ParsePriorityExtensions(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var token in input.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = token.Trim().ToLowerInvariant();
            if (t.Length > 1 && t.StartsWith('.') && seen.Add(t))
                result.Add(t);
        }
        return result;
    }
}

// Global priority gate: blocks non-priority file copies until all priority files across all jobs are done
public sealed class PriorityGate
{
    private int _pending;
    private readonly object _sync = new();

    // Add count of priority files before any copy starts (called from BackupExecutor, single-threaded)
    public void Add(int count)
    {
        lock (_sync) { _pending += count; }
    }

    // Signal that one priority file has been fully processed (copy + encrypt + log)
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

    // Block the calling thread until pendingPriority == 0; checks shouldStop every 200 ms.
    // If shouldPause fires, keeps waiting (no return/failed) until the pause clears.
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
    // If shouldPause fires while waiting, yields without holding the semaphore so
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

// Intercepts updates to report progress to popup progress window
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
        // Forward to inner state manager
        _innerStateManager.UpdateJobState(job, state);

        // Report progress to UI callback
        _progressCallback(job.Name, state.Progression, false);
    }

    public void SaveState()
    {
        _innerStateManager.SaveState();
    }
}
