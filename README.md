# EasySave

Backup software for Windows - ProSoft Project CESI

> **[Technical Documentation](TECHNICAL_DOC.md)** - Models, interfaces, services, JSON formats

## Description

EasySave is a console application that lets you create and run backup jobs. It supports:
- **Full backup**: copies all files
- **Differential backup**: copies only modified files

You can manage up to 5 backup jobs and run them individually or in batch.

## Requirements

- Windows 10/11
- .NET 8.0

## Run the project

```
git clone https://github.com/thiz68/FISA3_2026_G1_GABUS.git
cd FISA3_2026_G1_GABUS/EasySave
dotnet run --project EasySave.Console
```

Or if you have the built exe:
```
EasySave.Console.exe
```

## Usage

### Interactive mode

Just run the app without arguments and you'll see the menu:

```
=== EasySave v1.0 ===

1. Create backup job
2. List backup jobs
3. Execute backup
4. Change language
5. Exit

Enter your choice:
```

### Command line mode

You can also run jobs directly:

```
EasySave.Console.exe 1       # runs job 1
EasySave.Console.exe 1-3     # runs jobs 1, 2, 3
EasySave.Console.exe "1;3"   # runs jobs 1 and 3
```

## Architecture

### Solution structure

```
EasySave/
├── EasySave.slnx                 # Solution file
│
├── EasySave.Console/             # Console application (entry point)
│   ├── Program.cs                # Main entry point
│   └── EasySave.Console.csproj
│
├── EasySave.Core/                # Core business logic library
│   ├── Enums/
│   │   └── SaveType.cs           # Backup type enumeration
│   ├── Interfaces/
│   │   ├── IJob.cs               # Job contract
│   │   ├── IJobManager.cs        # Job management contract
│   │   ├── IConfigManager.cs     # Configuration contract
│   │   ├── IStateManager.cs      # State management contract
│   │   ├── ILocalizationService.cs # Localization contract
│   │   └── ILogger.cs            # Logging contract
│   ├── Models/
│   │   ├── SaveJob.cs            # Backup job model
│   │   ├── JobState.cs           # Real-time state model
│   │   └── LogEntry.cs           # Log entry model
│   ├── Services/
│   │   ├── JobManager.cs         # Job management implementation
│   │   ├── ConfigManager.cs      # Configuration persistence
│   │   ├── StateManager.cs       # Real-time state tracking
│   │   ├── LocalizationService.cs # Multi-language support
│   │   ├── BackupExecutor.cs     # Backup orchestration
│   │   └── FileBackupService.cs  # File copy operations
│   └── EasySave.Core.csproj
│
└── EasySaveLog/                  # Logging library (separate DLL)
    ├── Logger.cs                 # Daily JSON logging implementation
    └── EasySaveLog.csproj
```

### Project dependencies

```
EasySave.Console -> EasySave.Core
EasySave.Console -> EasySaveLog -> EasySave.Core
```

### Main components

- **JobManager**: Stores and manages backup jobs (max 5)
- **ConfigManager**: Saves/loads jobs to `config.json`
- **StateManager**: Tracks backup progress in real-time
- **BackupExecutor**: Orchestrates backup execution
- **FileBackupService**: Does the actual file copying (Full/Differential)
- **LocalizationService**: Handles EN/FR translations
- **Logger**: Writes daily log files in JSON format

### Data files

All data is stored in `%APPDATA%\EasySave\`:
- `config.json` - saved jobs configuration
- `state.json` - real-time backup state
- `Logs/YYYY-MM-DD.json` - daily transfer logs

## Limitations (v1.0)

- Max 5 jobs
- No file encryption
- No scheduler
- Sequential execution only
- Windows only

## What's next

Version 2.0 will add:
- GUI (WPF)
- Unlimited jobs
- File encryption
- Business software detection
- Multiple log formats (JSON/XML)

## Git workflow

We use `develop` as the main branch for development.

```
git checkout -b feature/my-feature
# do your work
git commit -m "Add: my feature"
git push origin feature/my-feature
# then create a PR to develop
```

Commit prefixes: `Add:`, `Fix:`, `Update:`, `Remove:`

## FAQ

**Where are my files stored?**
In `%APPDATA%\EasySave\`

**Full vs Differential?**
Full copies everything. Differential only copies files that changed since the last backup (compares modification dates).

**Why only 5 jobs?**
It's a v1.0 requirement from the project specs.

## Troubleshooting

- `Maximum 5 jobs allowed` -> Delete a job first
- `Job name already exists` -> Pick another name
- Files not copying (Differential) -> Check that source files are newer
- Permission denied -> Run as admin

---

FISA3 2026 - Group 1 GABUS | CESI
