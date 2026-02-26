# EasySave — Technical Documentation

---

## Table of Contents

1. [Global Architecture](#1-global-architecture)
2. [Core Models](#2-core-models)
3. [Interfaces](#3-interfaces)
4. [Services](#4-services)
5. [Execution Flow](#5-execution-flow)
6. [Concurrency Model](#6-concurrency-model)
7. [Persistence Layer](#7-persistence-layer)
8. [Logging System](#8-logging-system)
9. [Localization System](#9-localization-system)
10. [Unit Testing Strategy](#10-unit-testing-strategy)

---

## 1. Global Architecture

### Layered structure

The solution is organized into seven projects with a strict dependency hierarchy. No project in a lower layer references one above it.

```
Presentation layer      EasySave.WPF          (WPF, net8.0-windows)
                        EasySave.Console      (console, net8.0)

Application layer       EasySave.Core         (class library, net8.0)
                        EasySaveLog           (class library, net8.0)

Infrastructure          EasySave.LogServer    (console / Docker, net8.0)
                        CryptoSoft            (console, net8.0)

Tests                   EasySave.Tests        (xUnit, net10.0)
```

### Project dependency graph

```
EasySave.WPF
  ├── EasySave.Core
  └── EasySaveLog
        └── EasySave.Core

EasySave.Console
  ├── EasySave.Core
  └── EasySaveLog
        └── EasySave.Core

EasySave.LogServer
  ├── EasySave.Core
  └── EasySaveLog

EasySave.Tests
  └── EasySave.Core

CryptoSoft
  (no internal references)
```

### Separation of concerns

- **EasySave.Core** owns all domain logic: job management, backup execution, file copying, state tracking, configuration, localization, and path validation. It has no WPF dependency and no UI code.
- **EasySaveLog** owns the logging abstraction and its two transport implementations (local file, remote TCP). It depends on Core for `LogEntry` and `ILogger`.
- **EasySave.WPF** contains only presentation code: ViewModels, Views, Commands, Converters, and theme management. It creates and wires service instances manually.
- **CryptoSoft** is an independent executable. The Core library invokes it as a child process; it is not referenced as a project.

---

## 2. Core Models

### `SaveJob` (implements `IJob`)

Represents the configuration of one backup job.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Unique identifier for the job |
| `SourcePath` | `string` | Absolute path to the source directory |
| `TargetPath` | `string` | Absolute path to the backup destination |
| `Type` | `string` | `"full"` or `"diff"` |

Default value for `Type` is `"full"`. All properties default to empty strings.

### `AppSettings`

Persisted to `settings.json`. Loaded on each backup execution; not cached.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Language` | `string` | `"en"` | `"en"` or `"fr"` |
| `LogFormat` | `string` | `"json"` | `"json"` or `"xml"` |
| `ExtensionsToEncrypt` | `string` | `""` | Semicolon-separated dot-prefixed extensions |
| `BusinessSoftware` | `string` | `""` | Semicolon-separated process names |
| `PriorityExtension` | `string` | `""` | Semicolon-separated dot-prefixed extensions, order-significant |
| `LargeFileThresholdKB` | `long` | `0` | Threshold in KB; `0` disables the gate |
| `LogStorageMode` | `LogStorageMode` | `LocalOnly` | `0` Local, `1` Remote, `2` Both |
| `LogServerIp` | `string` | `"127.0.0.1"` | IP address of the remote log server |
| `LogServerPort` | `int` | `5000` | TCP port of the remote log server |

### `JobState`

Written to `states.json` after each file transfer during backup. One record per job.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Job name |
| `JobSourcePath` | `string` | Job source directory |
| `JobTargetPath` | `string` | Job target directory |
| `Timestamp` | `DateTime` | Last update time |
| `State` | `string` | Localized state string: Active, Completed, Failed, Inactive |
| `TotalFilesToCopy` | `int` | Total eligible files at job start |
| `TotalFilesSize` | `long` | Total eligible bytes at job start |
| `NbFilesLeftToDo` | `int` | Files not yet processed |
| `NbSizeLeftToDo` | `long` | Bytes not yet processed |
| `Progression` | `double` | Percentage complete (0–100), two decimal places |
| `CurrentSourceFilePath` | `string` | Absolute path of the file currently being copied |
| `CurrentTargetFilePath` | `string` | Absolute path of the in-progress target file |

### `LogEntry`

One record per file transfer, written to the daily log file.

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | Transfer start time |
| `JobName` | `string` | Name of the originating job |
| `SourceFile` | `string` | Absolute source path |
| `TargetFile` | `string` | Absolute target path |
| `FileSize` | `long` | File size in bytes |
| `TransferTimeMs` | `long` | Duration in ms; negative value indicates failure |
| `EncryptionTimeMs` | `long` | Encryption duration in ms; negative = failed or not applicable |
| `MachineName` | `string` | `Environment.MachineName` at log time |
| `UserName` | `string` | `Environment.UserName` at log time |

### `LogStorageMode` (enum)

```csharp
public enum LogStorageMode
{
    LocalOnly = 0,
    RemoteOnly = 1,
    LocalAndRemote = 2
}
```

---

## 3. Interfaces

### `IJob`

Defines the data contract for a backup job. `SaveJob` is the sole implementation. Using an interface allows mock objects in tests without requiring file system access.

```csharp
public interface IJob
{
    string Name { get; set; }
    string SourcePath { get; set; }
    string TargetPath { get; set; }
    string Type { get; set; }
}
```

### `IJobManager`

Governs the in-memory job collection.

```csharp
public interface IJobManager
{
    IReadOnlyList<IJob> Jobs { get; }
    int MaxJobs { get; }            // constant: 5
    void AddJob(IJob job);
    void RemoveJob(string name);
    IJob GetJob(int index);         // 1-based
    IJob GetJob(string name);
}
```

### `IConfigManager`

Abstracts the persistence of job lists. ConfigManager also exposes `LoadSettings()` and `SaveSettings()` directly (not on the interface) because settings management is not consumed through the interface boundary.

```csharp
public interface IConfigManager
{
    void LoadJobs(IJobManager manager);
    void SaveJobs(IJobManager manager);
}
```

### `IStateManager`

Consumed by `FileBackupService` to write execution state after each file. Abstracted to allow the WPF layer to inject a decorator (`ProgressTrackingStateManager`) that intercepts updates to feed the progress window.

```csharp
public interface IStateManager
{
    void UpdateJobState(IJob job, JobState state);
    void SaveState();
}
```

### `ILogger` / `ILogReader` / `ILogWriter`

`ILogger` is the primary logging interface consumed by `FileBackupService` and `BackupExecutor`. It composes `ILogReader` (for content retrieval in the Dashboard view) and exposes configuration and reachability checks.

```csharp
public interface ILogReader
{
    Task<string> ReadCurrentLogAsync();
}

public interface ILogWriter
{
    Task WriteAsync(LogEntry entry, string format = "json");
}

public interface ILogger : ILogReader
{
    void LogFileTransfer(DateTime, string, string, string, long, long, long);
    Task LogFileTransferAsync(DateTime, string, string, string, long, long, long);
    void SetLogFormat(string format);
    string GetCurrentLogFormat();
    LogStorageMode GetLogStorageMode();
    void Initialize();
    void LogBusinessSoftwareStop(DateTime, string, string);
    Task<bool> IsRemoteServerReachableAsync();
}
```

### `ILocalizationService`

Consumed by every layer that surfaces user-facing strings. The event-based change notification allows ViewModels to refresh all bound string properties without re-instantiation.

```csharp
public interface ILocalizationService
{
    string GetString(string key);
    void SetLanguage(string languageCode);
    string CurrentLanguage { get; }
    event EventHandler? LanguageChanged;
}
```

---

## 4. Services

### `JobManager`

Holds the in-memory list of `IJob` instances. `MaxJobs` is defined as 5; the enforcement check in `AddJob` is currently disabled in the implementation, so no hard cap is applied at runtime. `AddJob` throws `InvalidOperationException` on duplicate names. `GetJob(int)` uses 1-based indexing consistent with the console UI.

### `ConfigManager`

Reads and writes two JSON files in `AppDomain.CurrentDomain.BaseDirectory`:

- `config.json` — array of `SaveJob` objects (job definitions)
- `settings.json` — single `AppSettings` object

Uses `System.Text.Json` with `JsonSerializerOptions` for readable output. Missing files produce default objects rather than exceptions. `SaveSettings` and `LoadSettings` are not part of `IConfigManager` and are called directly from `SettingsViewModel` and `BackupExecutor`.

### `StateManager`

Maintains a `Dictionary<string, JobState>` keyed by job name. `UpdateJobState` acquires a lock, updates the dictionary, then calls `SaveState`. `SaveState` serializes the dictionary values to `states.json`. Thread safety is provided by a dedicated `_stateLock` object (not a SemaphoreSlim; state writes are synchronous and short).

### `LocalizationService`

Holds two hard-coded `Dictionary<string, Dictionary<string, string>>` instances for `"en"` and `"fr"`. Both dictionaries contain over 160 keys covering all console and WPF surfaces. `GetString` returns the key itself if no translation is found, preventing null reference errors in the UI. Language changes fire `LanguageChanged`, which ViewModels subscribe to in order to call `UpdateLocalizedStrings()`.

### `PathValidator`

Stateless helper. `IsSourceValid` checks that the directory exists and is not the same as the executable's directory. `IsTargetValid` checks that the path is absolute and that `Directory.CreateDirectory` can be called without throwing.

### `BusinessSoftwareChecker`

Calls `Process.GetProcessesByName` for each semicolon-separated token in the configured string. Returns `true` if any named process has at least one running instance. Used both as a pre-check before backup starts and polled every 500 ms during execution in `BackupProgressViewModel`.

### `CryptoSoftRunner`

Locates `CryptoSoft.exe` in the application directory. `EncryptFile(path)` starts the process with the file path as the sole argument, waits for exit, and returns elapsed milliseconds. A non-zero exit code or an exception returns a negative value. The caller (inside `CopyFile`) logs this value as `EncryptionTimeMs`.

### `FileBackupService`

The core file-operation class. Two public methods:

#### `CountPriorityFiles`

Pre-scan used by `BackupExecutor` before any thread starts copying. Accepts `IReadOnlyList<string> priorityExtensions` and counts eligible files matching any extension in the list. For differential jobs, a file is not counted if the target already exists with an equal or newer modification time.

```csharp
public int CountPriorityFiles(
    string sourceDir,
    string targetDir,
    string jobType,
    IReadOnlyList<string> priorityExtensions)
```

#### `CopyDirectory`

Orchestrates the two-phase backup for one job. Returns `true` on full success, `false` on any error or stop signal.

```csharp
public bool CopyDirectory(
    string sourceDir,
    string targetDir,
    IJob job,
    ILogger logger,
    IStateManager stateManager,
    ILocalizationService localization,
    Func<bool>? shouldStop = null,
    PriorityGate? gate = null,
    IReadOnlyList<string>? priorityExtensions = null,
    LargeFileGate? largeFileGate = null,
    Func<bool>? shouldPause = null)
```

Internally, the method:

1. Calls `CollectEligibleFiles` to apply the differential filter once up front.
2. Partitions eligible files into `priorityFilesOrdered` and `nonPriorityFiles`.
3. Executes Phase 1 (priority files in extension-list order), calling `gate.Done()` in a `finally` block for each file to prevent deadlock on early abort.
4. Drains any unprocessed priority gate slots if execution aborted early.
5. Waits at `gate.WaitIfBlocked()`.
6. Executes Phase 2 (non-priority files).

Both phases call `CopyFile` which runs the actual `File.Copy`, optionally invokes `CryptoSoftRunner`, and calls `logger.LogFileTransfer`. Large file gating is handled inside each phase: if the file exceeds `largeFileGate.ThresholdKB * 1024`, `Acquire` is called before copy and `Release` is called in a `finally` block.

### `BackupExecutor`

Manages parallel job execution and constructs the shared synchronization objects.

**`ExecuteSequential`** — used by the console application. Runs all jobs concurrently via `Task.Run` + `SemaphoreSlim`, waits for all to complete, returns a localization key string.

**`ExecuteWithProgress`** — used by the WPF application. Same concurrency model but accepts progress, completion, and pause-state callbacks. Wraps `IStateManager` in `ProgressTrackingStateManager` to intercept state updates.

**Pre-execution setup (identical in both methods):**

1. Load `AppSettings` from `ConfigManager`.
2. Call `ParsePriorityExtensions` to produce an ordered, deduplicated `List<string>`.
3. If the list is non-empty, instantiate `PriorityGate` and call `CountPriorityFiles` for every job to pre-load the counter.
4. If `LargeFileThresholdKB > 0`, instantiate `LargeFileGate`.

**`ParsePriorityExtensions`** splits the semicolon-delimited string, trims and lowercases each token, discards tokens that do not start with `.`, and deduplicates while preserving first-occurrence order. Tokens that lack a leading dot are silently skipped because UI validation prevents them from being saved.

**`MaxConcurrency`** is a static property: `Math.Clamp(Environment.ProcessorCount, 1, 8)`.

---

## 5. Execution Flow

### Application startup (WPF)

`App.xaml.cs` initializes services and constructs `MainViewModel`, which creates all child ViewModels. No dependency injection container is used; wiring is explicit in code. `LocalizationService.SetLanguage` is called with the persisted language before any UI is shown.

### Backup execution lifecycle

```
User triggers Execute (All or Selected)
  └── JobsViewModel.ExecuteSelected/AllCommand
        └── BusinessSoftwareChecker.IsBusinessSoftwareRunning
              ├── true  → show error, abort
              └── false → CryptoSoftRunner.IsCryptoSoftAvailable
                            ├── false → show warning, optionally abort
                            └── true  → open BackupProgressWindow
                                          └── BackupExecutor.ExecuteWithProgress(jobs, ...)
                                                ├── Parse priority extensions
                                                ├── Pre-scan: CountPriorityFiles × jobs → PriorityGate.Add(total)
                                                ├── Create LargeFileGate (if threshold > 0)
                                                └── For each job (parallel, max concurrency):
                                                      └── FileBackupService.CopyDirectory(...)
                                                            ├── Phase 1: priority files
                                                            ├── Wait: PriorityGate.WaitIfBlocked()
                                                            └── Phase 2: non-priority files
```

### Differential backup logic

`CollectEligibleFiles` applies the differential filter:

```
for each file in sourceDir (recursive):
    targetFile = targetDir + relativePath
    if jobType == "diff"
        AND targetFile exists
        AND targetFile.LastWriteTime >= sourceFile.LastWriteTime:
        skip
    else:
        include
```

The comparison uses `>=` on `LastWriteTime`: a target file with an equal or newer timestamp is considered up to date and excluded from the copy set.

### Priority file logic

`ParsePriorityExtensions` produces an ordered `List<string>` (e.g. `[".exe", ".pdf", ".zip"]`).

Within `CopyDirectory`, `priorityFilesOrdered` is constructed using LINQ `SelectMany` over the extension list:

```csharp
var priorityFilesOrdered = priorityExtensions
    .SelectMany(ext => allPriorityFiles
        .Where(f => Path.GetExtension(f.src).ToLowerInvariant() == ext))
    .ToList();
```

This preserves extension-list order while keeping the original directory enumeration order within each extension group. Files matching no priority extension go to `nonPriorityFiles` and are not processed until Phase 2.

### Business software pause

`WaitWhilePaused` polls `shouldPause` every 500 ms:

```csharp
while (shouldPause?.Invoke() == true)
{
    if (shouldStop?.Invoke() == true) return;
    Thread.Sleep(500);
}
```

This is called before directory creation and before each file copy in both phases. In the WPF layer, `BackupProgressViewModel` monitors `BusinessSoftwareChecker` on a background `Task` and updates the `shouldPause` delegate.

---

## 6. Concurrency Model

### Threading model

Each job runs on a `Task.Run` thread-pool thread. A single `SemaphoreSlim(maxConcurrency)` limits how many jobs execute simultaneously. `maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 8)`.

### PriorityGate

```csharp
public sealed class PriorityGate
{
    private int _pending;
    private readonly object _sync = new();

    public void Add(int count);
    public void Done();
    public void WaitIfBlocked(Func<bool>? shouldStop, Func<bool>? shouldPause);
}
```

`_pending` is the global count of priority-eligible files across all jobs. It is loaded once before any thread begins copying. Each call to `Done()` (made in a `finally` block after each priority file) decrements `_pending`. When `_pending` reaches zero, `Monitor.PulseAll` wakes all threads blocked at `WaitIfBlocked`.

`WaitIfBlocked` uses `Monitor.Wait` with a 200 ms timeout to remain responsive to `shouldStop` and `shouldPause` signals:

```csharp
while (_pending > 0)
{
    if (shouldPause?.Invoke() == true) { Monitor.Wait(_sync, 200); continue; }
    if (shouldStop?.Invoke() == true) return;
    Monitor.Wait(_sync, 200);
}
```

If a job aborts early (stop signal or I/O error), any un-entered priority slots are drained in a `for` loop after the Phase 1 iteration: `for (int i = priorityProcessed; i < priorityFilesOrdered.Count; i++) gate.Done()`. This prevents other jobs from blocking indefinitely at `WaitIfBlocked`.

### LargeFileGate

```csharp
public sealed class LargeFileGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public long ThresholdKB { get; }

    public bool Acquire(Func<bool>? shouldStop, Func<bool>? shouldPause);
    public void Release();
}
```

A binary semaphore (initial count 1) ensures at most one large file transfer proceeds at any given time across all jobs. Files at or below the threshold bypass this gate entirely.

`Acquire` loops with a 200 ms semaphore wait timeout:

```csharp
while (true)
{
    if (shouldStop?.Invoke() == true) return false;
    if (shouldPause?.Invoke() == true) { Thread.Sleep(200); continue; }
    if (_semaphore.Wait(200)) return true;
}
```

When paused, `Acquire` does not attempt to take the semaphore, allowing other non-paused jobs to use the bandwidth slot. `Release` is always called in a `finally` block to prevent semaphore leaks.

### ProgressTrackingStateManager

A decorator over `IStateManager`. Intercepts every `UpdateJobState` call to invoke the UI progress callback before delegating to the inner state manager. This avoids any WPF-specific code entering the Core layer.

---

## 7. Persistence Layer

All files are stored in `AppDomain.CurrentDomain.BaseDirectory` (the directory containing the running executable).

### `config.json`

JSON array of `SaveJob` objects. Loaded at startup into `JobManager`. Saved on every add, remove, or modify operation.

```json
[
  {
    "Name": "string",
    "SourcePath": "string",
    "TargetPath": "string",
    "Type": "full | diff"
  }
]
```

### `settings.json`

Single `AppSettings` object. Loaded fresh on each backup execution by `BackupExecutor` and on settings panel open by `SettingsViewModel`. Saved only when the user explicitly clicks Save in the settings panel.

### `states.json`

JSON array of `JobState` objects. Written after every file transfer by `StateManager.SaveState()`. Reflects the last known state of every job; not cleared between runs. State strings are localized (values like `"Active"`, `"Completed"`, `"Failed"` depend on the active language at write time).

### Log files

Written to `Logs/` subdirectory, created automatically on logger initialization. Named `YYYY-MM-DD.json` or `YYYY-MM-DD.xml`. The naming and format are derived from `AppSettings.LogFormat` at the time of each write. Rotation is implicit: the date in the filename changes at midnight.

Remote log server stores files in `CentralLogs/` using the same naming convention.

---

## 8. Logging System

### Local log writer (`LocalLogWriter`)

Uses a `SemaphoreSlim(1, 1)` to serialize concurrent write attempts. Each write performs a full read-deserialize-append-serialize-write cycle on the daily file. This is intentionally simple but is not atomic at the OS level; concurrent crashes could corrupt the file.

JSON writes use `System.Text.Json`; XML writes use `XmlSerializer` with an `ArrayOfLogEntry` root element.

### Remote log writer (`RemoteLogWriter`)

Opens a TCP connection to the configured host and port for each write attempt. The wire format is:

```
LOG|format|{json_serialized_LogEntry}
```

Connection timeout: 2000 ms. Retry count: 3, with 100 ms delays between attempts. On all retries exhausted, the error is propagated to `CompositeLogWriter`, which raises `RemoteServerUnreachable`.

### Composite log writer (`CompositeLogWriter`)

When `LogStorageMode` is `LocalAndRemote`, writes first to local (guaranteed), then attempts remote. Raises a `RemoteServerUnreachable` event if remote fails; callers subscribe to show a warning in the UI.

### Remote log reader (`RemoteLogReader`)

Sends `GET_LOG|format` over TCP. The server responds with a Base64-encoded byte array of the log file content. The reader decodes and returns the string. Timeout: 2000 ms.

### Logger (`EasySaveLog.Logger`)

Entry point for all logging calls. Constructs the appropriate reader/writer combination based on `LogStorageMode`. Exposes both synchronous (`LogFileTransfer`) and asynchronous (`LogFileTransferAsync`) variants. The async variant is used in the WPF execution path to avoid blocking the thread-pool thread during TCP writes.

`LogBusinessSoftwareStop` writes a synthetic `LogEntry` with `FileSize = -1` and both time fields set to `-1` to mark a pause event in the log without a real file transfer record.

`Initialize()` creates the `Logs/` directory if absent and verifies connectivity to the remote server if the storage mode includes remote.

---

## 9. Localization System

### Language data

`LocalizationService` contains two inline dictionaries (`"en"` and `"fr"`), each with over 160 keys. Keys cover every user-visible string in both the console and WPF surfaces: menu items, error messages, state labels, button labels, section headings, tooltip hints, and validation errors.

There is no external resource file; all strings are compiled into the assembly. This simplifies deployment at the cost of recompilation for new translations.

### Language switching

`SetLanguage(code)` checks that the code exists in the dictionary before assigning it and fires `LanguageChanged`. ViewModels subscribe in their constructor:

```csharp
localization.LanguageChanged += (_, _) => UpdateLocalizedStrings();
```

`UpdateLocalizedStrings()` re-reads every localized property via `GetString`. Because all properties implement `INotifyPropertyChanged`, the UI updates automatically.

The selected language is persisted to `settings.json` and applied before the WPF window appears, so the UI starts in the correct language without flicker.

### Missing key fallback

`GetString` returns the key itself if no entry is found in the active dictionary. This prevents null reference exceptions in bindings and makes missing translations visible in the UI.

---

## 10. Unit Testing Strategy

### Framework and tooling

- **xUnit 2.7.0** — test runner and assertion library
- **Moq 4.20.70** — interface mocking
- **coverlet** — coverage collection (integrated with `dotnet test`)
- **Target framework:** net10.0

### Test structure

Tests are organized under `EasySave.Tests/Services/`, one file per production class under test. The test project references only `EasySave.Core`; it has no WPF or logging dependency.

### Mocking approach

Tests that involve `FileBackupService` or `BackupExecutor` inject mock implementations of `ILogger`, `IStateManager`, and `ILocalizationService` via Moq. Mock `ILocalizationService` instances return empty strings for all `GetString` calls by default; specific keys are set up where behavior depends on them.

No real file system access is required for the core service tests. `BackupExecutorTests` that exercise actual file operations create temporary directories or use paths known to fail, ensuring assertions on return values and callback invocations without persistent side effects.

### Coverage areas

| Test class | Area covered |
|------------|--------------|
| `JobManagerTests` | Duplicate name rejection, index-out-of-range, name-not-found, add/remove correctness, `MaxJobs` constant |
| `BackupExecutorTests` | Empty job list returns completed, invalid source path returns failed, stop signal aborts execution, `MaxConcurrency` within bounds, progress callbacks invoked |
| `LocalizationServiceTests` | Key retrieval in both languages, missing key fallback, language switch event |
| `StateManagerTests` | State persistence to file, concurrent update safety |
| `PathValidatorTests` | Source existence check, executable directory exclusion, target path validity |
| `BackupListFormatterTests` | Formatted output for various job list states |
| `LocalLogReaderTests` | Daily log file read |
| `RemoteLogReaderTests` | Remote TCP log retrieval |
| `CryptoSoftRunnterTests` | Availability check (no executable present), encryption invocation |

### Running the tests

```bash
cd EasySave
dotnet test
```

Coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```
