/*
 * StateManager: persists the real-time state of all backup jobs to states.json.
 * All write operations are serialized under a single lock (_stateLock) to prevent
 * data races between concurrent backup threads. ReadStateFileContent is not locked
 * since it is used only for dashboard display (read-only, stale data is acceptable).
 */
namespace EasySave.Core.Services;

using System.Text.Json;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

public class StateManager : IStateManager
{
    private readonly string _stateFilePath;

    // Key = job name, Value = current job state. Protected by _stateLock.
    private readonly Dictionary<string, JobState> _states = new();

    // Lock serializes concurrent UpdateJobState calls from parallel backup threads.
    private readonly object _stateLock = new object();

    public StateManager()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _stateFilePath = Path.Combine(appDirectory, "states.json");
    }

    // Thread-safe update: sets job metadata, timestamps, and immediately persists to disk.
    public void UpdateJobState(IJob job, JobState state)
    {
        lock (_stateLock)
        {
            state.Name = job.Name;
            state.JobSourcePath = job.SourcePath;
            state.JobTargetPath = job.TargetPath;
            state.Timestamp = DateTime.Now;

            _states[job.Name] = state;
            SaveState();
        }
    }

    // Serialize all job states to states.json with pretty-printing.
    // Must be called inside _stateLock; IOException is swallowed (e.g., USB drive removed).
    public void SaveState()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_states.Values.ToList(), options);

        try
        {
            File.WriteAllText(_stateFilePath, json);
        }
        catch (IOException)
        {
            // Skip — drive may have become unavailable mid-backup.
        }
    }

    // Read the current state file content for dashboard display (not thread-safe, best-effort).
    public string ReadStateFileContent()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                return File.ReadAllText(_stateFilePath);
            }
        }
        catch (IOException)
        {
            // Ignore read errors.
        }
        return string.Empty;
    }
}
