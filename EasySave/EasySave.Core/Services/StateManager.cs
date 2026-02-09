namespace EasySave.Core.Services;

using System.Text.Json;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

//Manages real Time state of backup jobs, write state.json

public class StateManager : IStateManager
{
    private readonly string _stateFilePath;
    
    //Dictionary to store the state of each job
    //Key = job name, Value = job state information
    private readonly Dictionary<string, JobState> _states = new();
    
    //Constructor

    public StateManager()
    {
        //Get the application's directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        _stateFilePath = Path.Combine(appDirectory, "states.json");
    }
    
    //Update state for job
    public void UpdateJobState(IJob job, JobState state)
    {
        //Copy job information
        state.Name = job.Name;
        state.JobSourcePath = job.SourcePath;
        state.JobTargetPath = job.TargetPath;

        //Record update
        state.Timestamp = DateTime.Now;

        //Record or update state in dictionary
        _states[job.Name] = state;
        
        SaveState();
    }
    
    // Save all job states to the JSON file
    public void SaveState()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        //Dictionaries to Json
        var json = JsonSerializer.Serialize(_states.Values.ToList(), options);

        // Try to write file, catch errors if drive becomes unavailable (USB unplugged, etc.)
        try
        {
            File.WriteAllText(_stateFilePath, json);
        }
        catch (IOException)
        {
            // File write failed, probably due to drive issue - we just skip saving state
        }
    }
}