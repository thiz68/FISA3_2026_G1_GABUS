# EasySave

**EasySave** is a Windows backup utility developed. It provides a graphical desktop interface for creating, configuring, and executing backup jobs with support for concurrent execution, priority-based file ordering, selective encryption, and remote log collection.

> Technical documentation: [TECHNICAL_DOC.md](TECHNICAL_DOC.md)

---

## Features

- **Full and differential backup** — full copies everything; differential copies only files newer than their target counterpart
- **Concurrent execution** — runs multiple jobs in parallel, bounded by logical processor count (1–8 threads)
- **Priority file extensions** — semicolon-separated list of extensions (e.g. `.exe;.pdf`) processed before all other files across all concurrent jobs
- **Large file throttle** — configurable KB threshold above which at most one file transfer runs at a time, preventing bandwidth saturation
- **File encryption** — integrates with CryptoSoft (AES-256) to encrypt files matching configured extensions after copy
- **Business software detection** — suspends active backups when a configured process is running; resumes automatically when the process closes
- **Log output** — daily transfer logs in JSON or XML, written locally and/or streamed to a remote TCP log server
- **Remote log server** — standalone TCP server deployable via Docker, receives and stores logs centrally
- **Real-time state file** — `states.json` updated after every file transfer, readable by external monitoring tools
- **Dual-language UI** — English and French, switchable at runtime without restart
- **Light/dark theme** — macOS-inspired theme system switchable from the settings panel
- **WPF desktop GUI** — sidebar navigation, card-based settings, modal progress window per execution
- **Legacy console interface** — fully functional menu-driven console application retained alongside the GUI
- **Unit test suite** — 36 tests across 9 test classes using xUnit and Moq

---

## Architecture Overview

The solution is structured in independent layers. The WPF application is the primary interface; the console application shares the same core library.

```
EasySave.WPF          — primary desktop application (MVVM, net8.0-windows)
EasySave.Console      — legacy menu-driven console interface (net8.0)
EasySave.Core         — business logic, models, interfaces, services (net8.0)
EasySaveLog           — logging abstraction: local and remote writers (net8.0)
EasySave.LogServer    — standalone TCP log server, Docker-compatible (net8.0)
EasySave.Tests        — xUnit + Moq unit tests (net10.0)
CryptoSoft            — standalone AES-256 file encryption utility (net8.0)
```

Dependency graph (compile-time references):

```
EasySave.WPF
  └── EasySave.Core
  └── EasySaveLog
        └── EasySave.Core

EasySave.Console
  └── EasySave.Core
  └── EasySaveLog
        └── EasySave.Core

EasySave.LogServer
  └── EasySave.Core
  └── EasySaveLog

EasySave.Tests
  └── EasySave.Core
```

---

## Technologies Used

| Component | Technology |
|-----------|-----------|
| Desktop UI | WPF (.NET 8, MVVM) |
| Core library | C# 12 / .NET 8 |
| Console interface | .NET 8 console |
| Encryption | AES-256 via `System.Security.Cryptography.Aes` |
| Serialization | `System.Text.Json`, `System.Xml.Serialization` |
| Remote logging | Raw TCP sockets (`System.Net.Sockets`) |
| Containerization | Docker (LogServer only) |
| Unit tests | xUnit 2.7, Moq 4.20, coverlet |

---

## Requirements

- Windows 10 or 11
- .NET 8.0 SDK (build) or .NET 8.0 Runtime (run)
- CryptoSoft.exe must be present in the application directory if file encryption is enabled
- Docker (optional, for the remote log server)

---


## Usage

### WPF Interface

The application opens to a sidebar-navigated window with four panels:

- **Dashboard** — live preview of `states.json` and the current daily log file; displays remote server connectivity status
- **Backup Jobs** — create, edit, delete, and execute backup jobs; supports selecting individual jobs or running all
- **Settings** — configure log format, encryption extensions, business software, priority extensions, large file threshold, language, theme, and log server connection
- **Activity** — log file viewer for the current day

During a backup execution, a modal progress window shows per-job progress bars, thread count, and pause/stop controls.

### Console Interface

Launch the console application to access the interactive menu:

```
=== EasySave v1.0 ===

1. Create backup job
2. Remove backup job
3. Modify backup job
4. List backup jobs
5. Execute backup
6. Change language
7. Change log format
8. Configure business software
9. Exit
```

---

## Project Structure

```
EasySave/
├── EasySave.slnx
├── compose.yaml                          # Docker Compose for LogServer
│
├── EasySave.Core/
│   ├── Interfaces/
│   │   ├── IJob.cs
│   │   ├── IJobManager.cs
│   │   ├── IConfigManager.cs
│   │   ├── IStateManager.cs
│   │   ├── ILocalizationService.cs
│   │   ├── ILogger.cs
│   │   ├── ILogReader.cs
│   │   └── ILogWriter.cs
│   ├── Models/
│   │   ├── SaveJob.cs
│   │   ├── JobState.cs
│   │   ├── LogEntry.cs
│   │   ├── AppSettings.cs
│   │   └── LogStorageMode.cs
│   └── Services/
│       ├── JobManager.cs
│       ├── ConfigManager.cs
│       ├── StateManager.cs
│       ├── LocalizationService.cs
│       ├── BackupExecutor.cs             # PriorityGate, LargeFileGate defined here
│       ├── FileBackupService.cs
│       ├── PathValidator.cs
│       ├── BusinessSoftwareChecker.cs
│       └── CryptoSoftRunner.cs
│
├── EasySave.WPF/
│   ├── ViewModels/
│   │   ├── BaseViewModel.cs
│   │   ├── MainViewModel.cs
│   │   ├── JobsViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   ├── DashboardViewModel.cs
│   │   ├── ActivityViewModel.cs
│   │   └── BackupProgressViewModel.cs
│   ├── Views/
│   │   ├── DashboardView.xaml
│   │   ├── JobsView.xaml
│   │   ├── SettingsView.xaml
│   │   ├── ActivityView.xaml
│   │   └── BackupProgressWindow.xaml
│   ├── Commands/
│   │   └── RelayCommand.cs
│   ├── Converters/
│   │   └── BackupTypeConverter.cs        # also contains Bool/StringToVisibility
│   ├── Themes/
│   │   ├── Light.xaml
│   │   └── Dark.xaml
│   ├── Styles/
│   │   └── Controls.xaml
│   ├── ThemeManager.cs
│   ├── App.xaml
│   └── MainWindow.xaml
│
├── EasySave.Console/
│   ├── Program.cs
│   └── MenuHandler.cs
│
├── EasySaveLog/
│   ├── Logger.cs
│   ├── LocalLogWriter.cs
│   ├── RemoteLogWriter.cs
│   ├── CompositeLogWriter.cs
│   ├── LocalLogReader.cs
│   └── RemoteLogReader.cs
│
├── EasySave.LogServer/
│   ├── Program.cs
│   └── Dockerfile
│
├── EasySave.Tests/
│   └── Services/
│       ├── JobManagerTests.cs
│       ├── BackupExecutorTests.cs
│       ├── LocalizationServiceTests.cs
│       ├── StateManagerTests.cs
│       ├── PathValidatorTests.cs
│       ├── LocalLogReaderTests.cs
│       ├── RemoteLogReaderTests.cs
│       ├── BackupListFormatterTests.cs
│       └── CryptoSoftRunnterTests.cs
│
└── CryptoSoft/
    ├── Program.cs
    └── AesEncryptionService.cs
```

---

## Configuration and Data Files

All data files are stored in the application binary directory (next to the executable).

### `config.json` — Job definitions

```json
[
  {
    "Name": "daily-docs",
    "SourcePath": "C:\\Users\\user\\Documents",
    "TargetPath": "D:\\Backups\\Documents",
    "Type": "diff"
  }
]
```

### `settings.json` — Application settings

```json
{
  "Language": "en",
  "LogFormat": "json",
  "ExtensionsToEncrypt": ".docx;.xlsx",
  "BusinessSoftware": "notepad",
  "PriorityExtension": ".exe;.pdf",
  "LargeFileThresholdKB": 1024,
  "LogStorageMode": 0,
  "LogServerIp": "127.0.0.1",
  "LogServerPort": 5000
}
```

`LogStorageMode`: `0` = LocalOnly, `1` = RemoteOnly, `2` = LocalAndRemote.

`PriorityExtension`: semicolon-separated, dot-prefixed extensions. Order defines processing priority within each job.

### `states.json` — Real-time execution state

Updated after every file transfer. Contains one entry per job with current file paths, remaining counts, and completion percentage.

---

## Logging System

Transfer logs are written daily. The file name is `YYYY-MM-DD.json` (or `.xml`) and rotates automatically at midnight.

Each log entry records:

| Field | Description |
|-------|-------------|
| `Timestamp` | ISO-8601 timestamp |
| `JobName` | Name of the backup job |
| `SourceFile` | Absolute source path |
| `TargetFile` | Absolute target path |
| `FileSize` | File size in bytes |
| `TransferTimeMs` | Transfer duration in ms (negative = failed) |
| `EncryptionTimeMs` | Encryption duration in ms (negative = failed or skipped) |
| `MachineName` | Host machine name |
| `UserName` | OS user name |

Local logs are written to `Logs/` in the application directory. Remote logs are streamed over TCP to the configured log server using the format `LOG|format|{json_entry}`.

---

## Concurrency and Priority System

### Parallel execution

Jobs are executed in parallel. The concurrency limit is clamped between 1 and 8 based on `Environment.ProcessorCount`. A `SemaphoreSlim` limits how many jobs run simultaneously.

### Priority file barrier (PriorityGate)

When priority extensions are configured, execution splits into two phases within each job:

1. **Phase 1** — priority files are copied in extension-list order (first extension in the list, then second, etc.)
2. **Phase 2** — non-priority files are processed only after all priority files across all concurrent jobs have completed

The barrier is implemented as a shared counter (`PriorityGate`). The counter is pre-loaded with the total count of priority-eligible files across all jobs before any thread begins copying. Each priority file completion decrements the counter; once it reaches zero, all blocked Phase 2 threads are released.

### Large file throttle (LargeFileGate)

When `LargeFileThresholdKB` is greater than zero, a binary semaphore (`LargeFileGate`) limits simultaneous transfers of large files to one at a time. Files at or below the threshold bypass this gate entirely. The gate applies during both Phase 1 and Phase 2.

---

## Unit Tests

```bash
cd EasySave
dotnet test
```

| Test class | Tests | Coverage area |
|------------|------:|---------------|
| `JobManagerTests` | 9 | Add, remove, retrieve jobs; duplicate name; index bounds |
| `BackupExecutorTests` | 5 | Empty job list; invalid source; stop signal; max concurrency; progress callbacks |
| `LocalizationServiceTests` | 7 | String retrieval, language switching, missing key fallback |
| `StateManagerTests` | 2 | State persistence, thread-safe updates |
| `PathValidatorTests` | 4 | Source existence, non-executable dir check, target path validity |
| `BackupListFormatterTests` | 5 | Job list formatting |
| `LocalLogReaderTests` | 1 | Local log file reading |
| `RemoteLogReaderTests` | 1 | Remote TCP log retrieval |
| `CryptoSoftRunnterTests` | 2 | Availability check, encryption execution |
| **Total** | **36** | |

Tests use Moq for interface mocking (`ILogger`, `IStateManager`, `ILocalizationService`). No external resources or file system access is required for core logic tests.

---

## Git Workflow

The project uses conventional commits and a feature-branch workflow.

```bash
git checkout -b feat/my-feature
# implement changes
git commit -m "feat(scope): short description"
git push origin feat/my-feature
# open a pull request to main
```

Commit prefixes: `feat`, `fix`, `chore`, `refactor`, `docs`, `test`.

---

## Limitations

- Windows only (WPF and certain path assumptions)
- No backup scheduler — execution is manual
- CryptoSoft uses a fixed AES key and IV; not suitable for production-grade security
- Log files are appended in full on each write (full read-modify-write cycle per entry)
- No undo or rollback mechanism for completed backups
- The 5-job maximum is defined but not strictly enforced at runtime
- Remote logging uses plain TCP with no authentication or TLS

---

## Future Improvements

- Job execution scheduler (time-based or event-based triggers)
- Incremental/mirror backup modes beyond full and differential
- Pluggable encryption with user-supplied keys
- Log server authentication and encrypted transport
- Persistent undo journal for backup operations
- REST API layer for external job management and monitoring

---

## Authors

FISA3 2026 — Group 1 — CESI Engineering School

Contributors: Thibaud, Hugo, Clery, Paul,

