# EasySave - Technical Documentation

## Summary

1. [Project Architecture](#project-architecture)
2. [Models](#models)
3. [Interfaces](#interfaces)
4. [Services](#services)
5. [JSON File Formats](#json-file-formats)
6. [How it works](#how-it-works)

---

## Project Architecture

The project is split into 3 parts:

- **EasySave.Console**: the app we run, with the menu and everything
- **EasySave.Core**: all the business logic (models, services, interfaces)
- **EasySaveLog**: a separate DLL just for logs (it was required in the specs)

Dependencies between projects:
- Console uses Core and EasySaveLog
- EasySaveLog uses Core (for the models)

---

## Models

### SaveJob

This is the model that represents a backup job. It contains:

```csharp
public string Name { get; set; }        // job name
public string SourcePath { get; set; }  // folder to backup
public string TargetPath { get; set; }  // where we put the backup
public SaveType Type { get; set; }      // Full or Differential
```

### JobState

This one is used to track the state of a running backup. It has quite a few properties:

- `Name`: job name
- `State`: "Active", "Inactive" or "Completed"
- `Progression`: percentage from 0 to 100
- `TotalFilesToCopy`: total number of files
- `TotalFilesSize`: total size in bytes
- `NbFilesLeftToDo`: how many files left
- `NbSizeLeftToDo`: how much size left
- `CurrentSourceFilePath` and `CurrentTargetFilePath`: the file currently being copied

### LogEntry

For logs, each file transfer is recorded with:

- `Timestamp`: when it happened
- `JobName`: which job
- `SourceFile` / `TargetFile`: the paths
- `FileSize`: file size
- `TransferTimeMs`: transfer time in ms (if negative = error)

### SaveType

It's just an enum:
- `Full` = 0: copies everything
- `Differential` = 1: copies only what changed

---

## Interfaces

We used interfaces so we can swap implementations easily (and it's cleaner).

**IJob**: contract for a job (Name, SourcePath, TargetPath, Type)

**IJobManager**: to manage the job list
- `Jobs`: read-only list
- `AddJob()`, `RemoveJob()`, `GetJob()`

**IConfigManager**: to save/load jobs
- `LoadJobs()`: loads from config.json
- `SaveJobs()`: saves to config.json

**IStateManager**: for real-time tracking
- `UpdateJobState()`: updates job state
- `SaveState()`: writes to state.json

**ILocalizationService**: for multi-language
- `GetString(key)`: returns translated text
- `SetLanguage("en" or "fr")`

**ILogger**: for logs
- `Initialize()`: creates log folder
- `LogFileTransfer()`: records a transfer

---

## Services

### JobManager

Manages the job list. Maximum 5 jobs (it's a v1.0 constraint).
- If we try to add a 6th one -> exception
- If the name already exists -> exception too

### ConfigManager

Saves jobs to `%APPDATA%\EasySave\config.json`. Uses System.Text.Json for serialization.

### StateManager

Writes real-time state to `%APPDATA%\EasySave\state.json`. It's updated for each file copied.

### BackupExecutor

This one orchestrates everything. It can:
- Run a single job
- Run multiple jobs in sequence
- Parse commands like "1-3" or "1;3"

### FileBackupService

Does the actual file copying. For differential mode, it compares modification dates:
- If target file exists AND it's newer or equal to source -> we skip
- Otherwise -> we copy

### LocalizationService

Handles EN/FR translations. Texts are stored in dictionaries hardcoded in the code.

### Logger (in EasySaveLog)

Writes logs to `%APPDATA%\EasySave\Logs\`. One file per day, named with the date (ex: 2024-01-15.json).

---

## JSON File Formats

All files are in `%APPDATA%\EasySave\`

### config.json

This is where we store jobs:

```json
[
  {
    "Name": "Backup Documents",
    "SourcePath": "C:\\Users\\<username>\\Documents",
    "TargetPath": "D:\\Backup\\Documents",
    "Type": 0
  }
]
```

Type = 0 for Full, 1 for Differential.

### state.json

Real-time state during a backup:

```json
[
  {
    "Name": "Backup Documents",
    "JobSourcePath": "C:\\Users\\<username>\\Documents",
    "JobTargetPath": "D:\\Backup\\Documents",
    "Timestamp": "2024-01-15T14:30:00",
    "State": "Active",
    "TotalFilesToCopy": 100,
    "TotalFilesSize": 50000000,
    "NbFilesLeftToDo": 45,
    "NbSizeLeftToDo": 22000000,
    "Progression": 55.0,
    "CurrentSourceFilePath": "C:\\Users\\<username>\\Documents\\test.docx",
    "CurrentTargetFilePath": "D:\\Backup\\Documents\\test.docx"
  }
]
```

### Logs (ex: 2024-01-15.json)

Each file transfer is logged:

```json
[
  {
    "Timestamp": "2024-01-15T14:30:01",
    "JobName": "Backup Documents",
    "SourceFile": "C:\\Users\\<username>\\Documents\\test.docx",
    "TargetFile": "D:\\Backup\\Documents\\test.docx",
    "FileSize": 15000,
    "TransferTimeMs": 45
  }
]
```

If TransferTimeMs is negative, it means the transfer failed.

---

## How it works

### At startup

1. We create all services (JobManager, ConfigManager, etc.)
2. We initialize the logger (creates the folder)
3. We load jobs from config.json
4. If there's command line arguments -> we run directly
5. Otherwise -> we show the menu

### During a backup

1. We parse the command (ex: "1-3" -> jobs 1, 2, 3)
2. For each job:
   - We set state to "Active"
   - We calculate file count and total size
   - We copy each file one by one
   - For each file: log + update state.json
   - At the end: state = "Completed"

### Differential mode

For each source file:
1. We check if the file exists in destination
2. If yes, we compare last modification dates
3. If destination is newer or equal -> we don't copy
4. Otherwise -> we copy

---

*FISA3 2026 - Group 1 GABUS | CESI*
