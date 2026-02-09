# EasySave - Diagrammes de Séquence

## 1. Initialisation de l'application

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant JobManager
    participant ConfigManager
    participant LocalizationService
    participant Logger
    participant StateManager
    participant BackupExecutor

    User->>Program: Main(args)

    Program->>JobManager: new JobManager()
    Program->>ConfigManager: new ConfigManager()
    Program->>LocalizationService: new LocalizationService()
    Program->>Logger: new Logger()
    Program->>StateManager: new StateManager()
    Program->>BackupExecutor: new BackupExecutor()

    Program->>Logger: Initialize()
    Logger->>Logger: CreateDirectory(Logs/)

    Program->>ConfigManager: LoadJobs(jobManager)
    ConfigManager->>ConfigManager: File.Exists(config.json)?

    alt config.json exists
        ConfigManager->>ConfigManager: File.ReadAllText()
        ConfigManager->>ConfigManager: JsonSerializer.Deserialize()
        loop Pour chaque job
            ConfigManager->>JobManager: AddJob(job)
        end
    end

    alt args.Length > 0
        Program->>Program: ExecuteCommand(args[0])
    else Mode interactif
        Program->>Program: ShowMenu()
    end
```

---

## 2. Création d'un job

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant Console
    participant LocalizationService
    participant JobManager
    participant ConfigManager

    User->>Program: Choix menu "1"
    Program->>Program: CreateJob()

    Program->>LocalizationService: GetString("enter_name")
    Program->>Console: Write(prompt)
    Console->>User: Affiche prompt
    User->>Console: Saisit nom
    Console->>Program: name

    Program->>JobManager: Jobs (vérif collision)

    alt Nom existe déjà
        Program->>LocalizationService: GetString("job_name_alr_exist")
        Program->>Console: WriteLine(erreur)
        Program-->>User: Fin (erreur)
    else Nom disponible
        Program->>LocalizationService: GetString("enter_source")
        Program->>Console: Write(prompt)
        User->>Console: Saisit source

        Program->>LocalizationService: GetString("enter_target")
        Program->>Console: Write(prompt)
        User->>Console: Saisit target

        Program->>LocalizationService: GetString("enter_type")
        Program->>Console: Write(prompt)
        User->>Console: Saisit type (1 ou 2)

        Program->>Program: new SaveJob(name, source, target, type)
        Program->>JobManager: AddJob(job)

        alt Jobs.Count >= MaxJobs
            JobManager-->>Program: InvalidOperationException
            Program->>Console: WriteLine(erreur)
        else OK
            JobManager->>JobManager: _jobs.Add(job)
            Program->>ConfigManager: SaveJobs(jobManager)
            ConfigManager->>ConfigManager: JsonSerializer.Serialize()
            ConfigManager->>ConfigManager: File.WriteAllText(config.json)
            Program->>Console: WriteLine("Job créé")
        end
    end
```

---

## 3. Suppression d'un job

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant Console
    participant LocalizationService
    participant JobManager
    participant ConfigManager

    User->>Program: Choix menu "2"
    Program->>Program: RemoveJobs()

    alt Jobs.Count == 0
        Program->>LocalizationService: GetString("job_list_empty")
        Program->>Console: WriteLine(message)
        Program-->>User: Fin
    else Jobs existent
        Program->>Program: ListJobs()
        Program->>Console: Affiche liste

        Program->>LocalizationService: GetString("job_to_remove")
        Program->>Console: Write(prompt)
        User->>Console: Saisit index ou nom
        Console->>Program: input

        alt input est un nombre
            Program->>JobManager: GetJob(index)
            JobManager-->>Program: job
        else input est un nom
            Program->>JobManager: Jobs.FirstOrDefault(name)
            JobManager-->>Program: job ou null
        end

        alt job non trouvé
            Program->>LocalizationService: GetString("error_not_found")
            Program->>Console: WriteLine(erreur)
        else job trouvé
            Program->>JobManager: RemoveJob(job.Name)
            JobManager->>JobManager: _jobs.Remove(job)

            Program->>ConfigManager: SaveJobs(jobManager)
            ConfigManager->>ConfigManager: JsonSerializer.Serialize()
            ConfigManager->>ConfigManager: File.WriteAllText()

            Program->>Console: WriteLine("Job supprimé")
        end
    end
```

---

## 4. Modification d'un job

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant Console
    participant LocalizationService
    participant JobManager
    participant ConfigManager

    User->>Program: Choix menu "3"
    Program->>Program: ModifyJobs()

    alt Jobs.Count == 0
        Program->>Console: "Aucun job"
        Program-->>User: Fin
    else Jobs existent
        Program->>Program: ListJobs()

        Program->>Console: Write("Job à modifier")
        User->>Console: Saisit index ou nom

        alt input est un nombre
            Program->>JobManager: GetJob(index)
        else input est un nom
            Program->>JobManager: Jobs.FirstOrDefault(name)
        end

        alt job non trouvé
            Program->>Console: "Erreur: non trouvé"
        else job trouvé
            Program->>Console: Write("Nouveau nom")
            User->>Console: newName

            Program->>JobManager: Jobs (vérif collision)

            alt Nom existe déjà
                Program->>Console: "Erreur: nom existe"
            else OK
                Program->>Console: Write("Nouvelle source")
                User->>Console: newSource

                Program->>Console: Write("Nouvelle cible")
                User->>Console: newTarget

                Program->>Console: Write("Nouveau type")
                User->>Console: newType

                Program->>Program: job.Name = newName
                Program->>Program: job.SourcePath = newSource
                Program->>Program: job.TargetPath = newTarget
                Program->>Program: job.Type = newType

                Program->>ConfigManager: SaveJobs(jobManager)
                Program->>Console: "Job modifié"
            end
        end
    end
```

---

## 5. Exécution d'une sauvegarde (flux principal)

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant BackupExecutor
    participant FileBackupService
    participant StateManager
    participant Logger

    User->>Program: Choix menu "5"
    Program->>Program: ExecuteBackup()
    Program->>Program: ListJobs()

    Program->>User: "Entrez les jobs (ex: 1 | 2-5 | 1;3;4)"
    User->>Program: command (ex: "1-3")

    Program->>Program: ExecuteCommand(command)
    Program->>BackupExecutor: ExecuteFromCommand(command, jobManager, logger, stateManager)

    BackupExecutor->>BackupExecutor: Parse command (split par ";")

    loop Pour chaque partie
        alt Format "X-Y" (range)
            BackupExecutor->>BackupExecutor: Extraire start et end
            BackupExecutor->>BackupExecutor: Ajouter indexes [start..end]
        else Format "X" (single)
            BackupExecutor->>BackupExecutor: Ajouter index
        end
    end

    BackupExecutor->>BackupExecutor: Valider bornes (1 <= index <= Jobs.Count)

    alt Index invalide
        BackupExecutor-->>Program: return false
        Program->>User: Erreur affichée
    else Indexes valides
        loop Pour chaque index
            BackupExecutor->>BackupExecutor: jobManager.GetJob(index)
        end

        BackupExecutor->>BackupExecutor: ExecuteSequential(jobs, logger, stateManager)

        loop Pour chaque job
            BackupExecutor->>BackupExecutor: ExecuteSingle(job, logger, stateManager)
            Note over BackupExecutor: Voir diagramme 6
        end

        BackupExecutor-->>Program: return true
        Program->>User: "Sauvegarde terminée"
    end
```

---

## 6. Exécution d'un job unique (ExecuteSingle)

```mermaid
sequenceDiagram
    participant BackupExecutor
    participant LocalizationService
    participant StateManager
    participant FileBackupService
    participant Logger
    participant FileSystem

    BackupExecutor->>LocalizationService: new LocalizationService()

    BackupExecutor->>BackupExecutor: new JobState()
    BackupExecutor->>LocalizationService: GetString("active")
    BackupExecutor->>BackupExecutor: state.State = "Active"

    BackupExecutor->>StateManager: UpdateJobState(job, state)
    StateManager->>StateManager: state.Name = job.Name
    StateManager->>StateManager: state.Timestamp = DateTime.Now
    StateManager->>StateManager: _states[job.Name] = state
    StateManager->>StateManager: SaveState()
    StateManager->>FileSystem: File.WriteAllText(states.json)

    BackupExecutor->>FileBackupService: CopyDirectory(source, target, job, logger, stateManager)
    Note over FileBackupService: Voir diagramme 7

    BackupExecutor->>LocalizationService: GetString("completed")
    BackupExecutor->>BackupExecutor: state.State = "Completed"
    BackupExecutor->>BackupExecutor: state.Progression = 100

    BackupExecutor->>StateManager: UpdateJobState(job, state)
    StateManager->>FileSystem: File.WriteAllText(states.json)
```

---

## 7. Copie de répertoire (FileBackupService)

```mermaid
sequenceDiagram
    participant FileBackupService
    participant FileSystem
    participant Logger
    participant StateManager

    FileBackupService->>FileBackupService: CalculateEligibleFiles(sourceDir, targetDir, type)

    loop Pour chaque fichier dans sourceDir (récursif)
        FileBackupService->>FileSystem: new FileInfo(file)

        alt type == "diff"
            FileBackupService->>FileSystem: File.Exists(targetFile)?
            FileBackupService->>FileSystem: GetLastWriteTime(source vs target)
            alt Target plus récent
                FileBackupService->>FileBackupService: Skip (continue)
            else Source plus récent
                FileBackupService->>FileBackupService: count++, size += fileSize
            end
        else type == "full"
            FileBackupService->>FileBackupService: count++, size += fileSize
        end
    end

    FileBackupService-->>FileBackupService: return (totalFiles, totalSize)

    FileBackupService->>FileSystem: Directory.CreateDirectory(targetDir)

    FileBackupService->>FileBackupService: CopyDirectoryRecursive(...)

    loop Pour chaque fichier dans dossier courant
        alt type == "diff" AND target existe AND target >= source
            FileBackupService->>FileBackupService: Skip
        else Copie nécessaire
            FileBackupService->>FileBackupService: CopyFile(source, target, job, logger, stateManager)
            Note over FileBackupService: Voir diagramme 8

            FileBackupService->>FileBackupService: filesRemaining--
            FileBackupService->>FileBackupService: sizeRemaining -= fileSize
            FileBackupService->>FileBackupService: progression = (copied/total) * 100

            FileBackupService->>FileBackupService: UpdateStateForFile(...)
            FileBackupService->>StateManager: UpdateJobState(job, state)
        end
    end

    loop Pour chaque sous-dossier
        FileBackupService->>FileSystem: Directory.CreateDirectory(targetSubDir)
        FileBackupService->>FileBackupService: CopyDirectoryRecursive(subDir, ...)
    end
```

---

## 8. Copie d'un fichier unique

```mermaid
sequenceDiagram
    participant FileBackupService
    participant FileSystem
    participant Stopwatch
    participant Logger

    FileBackupService->>FileSystem: new FileInfo(sourceFile)
    FileSystem-->>FileBackupService: fileInfo
    FileBackupService->>FileBackupService: fileSize = fileInfo.Length

    FileBackupService->>Stopwatch: StartNew()

    alt Copie réussie
        FileBackupService->>FileSystem: File.Copy(source, target, overwrite: true)
        FileBackupService->>Stopwatch: Stop()
        Stopwatch-->>FileBackupService: ElapsedMilliseconds

        FileBackupService->>Logger: LogFileTransfer(timestamp, jobName, source, target, fileSize, timeMs)

        Logger->>Logger: new LogEntry(...)
        Logger->>Logger: GetDailyLogFilePath()
        Logger-->>Logger: "Logs/2024-01-15.json"

        alt Log file exists
            Logger->>FileSystem: File.ReadAllText(logFile)
            Logger->>Logger: JsonSerializer.Deserialize()
        end

        Logger->>Logger: entries.Add(entry)
        Logger->>Logger: JsonSerializer.Serialize(entries)
        Logger->>FileSystem: File.WriteAllText(logFile, json)

    else Exception levée
        FileBackupService->>Stopwatch: Stop()
        FileBackupService->>Logger: LogFileTransfer(..., -timeMs)
        Note over Logger: timeMs négatif = échec
    end

    FileBackupService-->>FileBackupService: return fileSize
```

---

## 9. Changement de langue

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant Console
    participant LocalizationService

    User->>Program: Choix menu "6"
    Program->>Program: ChangeLanguage()

    Program->>Console: WriteLine("1. English")
    Program->>Console: WriteLine("2. Français")
    Program->>Console: Write("> ")

    User->>Console: Saisit choix
    Console->>Program: choice

    alt choice == "1"
        Program->>LocalizationService: SetLanguage("en")
        LocalizationService->>LocalizationService: _resources.ContainsKey("en")?
        LocalizationService->>LocalizationService: _currentLanguage = "en"
    else choice == "2"
        Program->>LocalizationService: SetLanguage("fr")
        LocalizationService->>LocalizationService: _resources.ContainsKey("fr")?
        LocalizationService->>LocalizationService: _currentLanguage = "fr"
    end

    Note over Program: Les prochains GetString() <br/> utiliseront la nouvelle langue
```

---

## 10. Chargement de la configuration (au démarrage)

```mermaid
sequenceDiagram
    participant Program
    participant ConfigManager
    participant FileSystem
    participant JsonSerializer
    participant JobManager

    Program->>ConfigManager: LoadJobs(jobManager)

    ConfigManager->>FileSystem: File.Exists(config.json)

    alt Fichier n'existe pas
        ConfigManager-->>Program: return (rien à charger)
    else Fichier existe
        ConfigManager->>FileSystem: File.ReadAllText(config.json)
        FileSystem-->>ConfigManager: json string

        ConfigManager->>JsonSerializer: Deserialize<List<SaveJob>>(json)
        JsonSerializer-->>ConfigManager: List<SaveJob> jobs

        alt jobs == null
            ConfigManager-->>Program: return
        else jobs valides
            loop Pour chaque job dans jobs
                ConfigManager->>JobManager: AddJob(job)
                JobManager->>JobManager: Vérif count < MaxJobs
                JobManager->>JobManager: Vérif nom unique
                JobManager->>JobManager: _jobs.Add(job)
            end
        end
    end
```

---

## 11. Sauvegarde de la configuration

```mermaid
sequenceDiagram
    participant Program
    participant ConfigManager
    participant JobManager
    participant JsonSerializer
    participant FileSystem

    Program->>ConfigManager: SaveJobs(jobManager)

    ConfigManager->>ConfigManager: new JsonSerializerOptions { WriteIndented = true }

    ConfigManager->>JobManager: Jobs (get)
    JobManager-->>ConfigManager: IReadOnlyList<IJob>

    ConfigManager->>JsonSerializer: Serialize(jobs, options)
    JsonSerializer-->>ConfigManager: json string (formatted)

    ConfigManager->>FileSystem: File.WriteAllText(config.json, json)

    Note over FileSystem: config.json mis à jour
```

---

## 12. Mise à jour de l'état (StateManager)

```mermaid
sequenceDiagram
    participant Caller
    participant StateManager
    participant JobState
    participant JsonSerializer
    participant FileSystem

    Caller->>StateManager: UpdateJobState(job, state)

    StateManager->>JobState: state.Name = job.Name
    StateManager->>JobState: state.JobSourcePath = job.SourcePath
    StateManager->>JobState: state.JobTargetPath = job.TargetPath
    StateManager->>JobState: state.Timestamp = DateTime.Now

    StateManager->>StateManager: _states[job.Name] = state

    StateManager->>StateManager: SaveState()

    StateManager->>ConfigManager: new JsonSerializerOptions { WriteIndented = true }
    StateManager->>StateManager: _states.Values.ToList()

    StateManager->>JsonSerializer: Serialize(statesList, options)
    JsonSerializer-->>StateManager: json string

    StateManager->>FileSystem: File.WriteAllText(states.json, json)
```
