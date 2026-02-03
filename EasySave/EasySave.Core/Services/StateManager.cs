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
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var stateDir = Path.Combine(appData, "G1_EasySave");
        
        Directory.CreateDirectory(stateDir);
        
        _stateFilePath = Path.Combine(stateDir, "states.json");
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
        
        File.WriteAllText(_stateFilePath, json);
    }
}