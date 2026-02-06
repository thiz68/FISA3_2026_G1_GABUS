# EasySave - Technical Documentation

## Summary

1. [Project Architecture](#project-architecture)
2. [Models](#models)
3. [Interfaces](#interfaces)
4. [Services](#services)
5. [JSON File Formats](#json-file-formats)
6. [How it works](#how-it-works)
7. [Unit Tests](#unit-tests)

---

## Project Architecture

The project is split into 4 parts:

- **EasySave.Console**: the app we run, with the menu and everything
- **EasySave.Core**: all the business logic (models, services, interfaces)
- **EasySaveLog**: a separate DLL just for logs (it was required in the specs)
- **EasySave.Tests**: unit tests using xUnit and Moq

Dependencies between projects:
- Console uses Core and EasySaveLog
- EasySaveLog uses Core (for the models)
- Tests uses Core

---

## Models

### SaveJob

This is the model that represents a backup job. It contains:

```csharp
public string Name { get; set; }        // job name
public string SourcePath { get; set; }  // folder to backup
public string TargetPath { get; set; }  // where we put the backup
public string Type { get; set; }        // "full" or "diff"
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

Saves jobs to `config.json` in the application directory. Uses System.Text.Json for serialization.

### StateManager

Writes real-time state to `states.json` in the application directory. It's updated for each file copied.

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

Writes logs to `Logs/` folder in the application directory. One file per day, named with the date (ex: 2024-01-15.json).

---

## JSON File Formats

All files are stored in the application directory (next to the executable).

### config.json

This is where we store jobs:

```json
[
  {
    "Name": "Backup Documents",
    "SourcePath": "C:\\Users\\<username>\\Documents",
    "TargetPath": "D:\\Backup\\Documents",
    "Type": "full"
  }
]
```

Type = "full" for Full backup, "diff" for Differential backup.

### states.json

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
   - For each file: log + update states.json
   - At the end: state = "Completed"

### Differential mode

For each source file:
1. We check if the file exists in destination
2. If yes, we compare last modification dates
3. If destination is newer or equal -> we don't copy
4. Otherwise -> we copy

---

## Unit Tests

Unit tests are located in `EasySave.Tests/` and use **xUnit** as the testing framework with **Moq** for mocking dependencies.

### Running tests

```bash
cd EasySave
dotnet test
```

### Test coverage

| Class | File | Tests |
|-------|------|-------|
| JobManager | JobManagerTests.cs | 10 |
| LocalizationService | LocalizationServiceTests.cs | 8 |
| BackupExecutor | BackupExecutorTests.cs | 5 |
| **Total** | | **23** |

### JobManagerTests (10 tests)

1. `AddJob_ValidJob_JobAddedToList` - Verifie qu'un job valide est ajoute a la liste
2. `AddJob_MaxJobsReached_ThrowsException` - Verifie qu'une erreur est levee si on depasse 5 jobs
3. `AddJob_DuplicateName_ThrowsException` - Verifie qu'une erreur est levee si le nom existe deja
4. `RemoveJob_ExistingJob_JobRemoved` - Verifie qu'un job existant est supprime
5. `RemoveJob_NonExistingJob_NoException` - Verifie que supprimer un job inexistant ne plante pas
6. `GetJob_ValidIndex_ReturnsJob` - Verifie que GetJob(index) retourne le bon job
7. `GetJob_ValidName_ReturnsJob` - Verifie que GetJob(nom) retourne le bon job
8. `GetJob_InvalidIndex_ThrowsException` - Verifie qu'un index invalide leve une erreur
9. `GetJob_InvalidName_ThrowsException` - Verifie qu'un nom invalide leve une erreur
10. `Jobs_ReturnsReadOnlyList` - Verifie que la liste des jobs est en lecture seule

### LocalizationServiceTests (8 tests)

1. `GetString_ExistingKey_ReturnsTranslation` - Verifie qu'une cle existante retourne sa traduction
2. `SetLanguage_ValidLanguage_ChangesLanguage` - Verifie que changer vers "fr" fonctionne
3. `SetLanguage_InvalidLanguage_KeepsCurrentLanguage` - Verifie qu'une langue invalide ne change rien
4. `GetString_AfterSetLanguageFr_ReturnsFrenchText` - Verifie que le texte est en francais apres SetLanguage("fr")
5. `GetString_MultipleLanguages_ReturnsCorrectTranslation` (x4) - Verifie plusieurs combinaisons langue/cle

### BackupExecutorTests (5 tests)

1. `ExecuteFromCommand_SingleIndex_CallsExecuteSingle` - Verifie que "1" execute le job 1
2. `ExecuteFromCommand_RangeFormat_ExecutesMultipleJobs` - Verifie que "1-3" execute les jobs 1 a 3
3. `ExecuteFromCommand_SemicolonFormat_ExecutesSelectedJobs` - Verifie que "1;3" execute les jobs 1 et 3
4. `ExecuteFromCommand_InvalidIndex_DoesNotExecute` - Verifie qu'un index invalide n'execute rien
5. `ExecuteFromCommand_InvalidFormat_DoesNotExecute` - Verifie qu'un format invalide n'execute rien

---

*FISA3 2026 - Group 1 GABUS | CESI*
