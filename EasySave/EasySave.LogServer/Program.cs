using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using EasySave.Core.Models;

class Program
{
    private static readonly int Port = 5000;
    private static readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    private static int _totalLogsReceived = 0;
    private static int _totalClientsConnected = 0;

    static async Task Main()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              G1 - EasySave Central Log Server                ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  Port: {Port,-55}                                           ║");
        Console.WriteLine($"║  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss,-51}            ║");
        Console.WriteLine($"║  Log Directory: CentralLogs/                                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();

        LogInfo($"Server listening on port {Port}...");
        LogInfo("Waiting for connections...");
        Console.WriteLine();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    private static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"SUCCESS: {message}");
        Console.ResetColor();
    }

    private static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.WriteLine($"WARNING: {message}");
        Console.ResetColor();
    }

    private static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.WriteLine($"ERROR: {message}");
        Console.ResetColor();
    }

    private static void LogData(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"    └── {label}: ");
        Console.ResetColor();
        Console.WriteLine(value);
    }

    private static async Task HandleClient(TcpClient client)
    {
        _totalClientsConnected++;
        var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

        LogInfo($"Client connected from {clientEndpoint} (Total connections: {_totalClientsConnected})");

        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                var message = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(message))
                {
                    LogWarning("Empty message received, ignoring.");
                    return;
                }

                var parts = message.Split('|');
                var command = parts[0];

                LogInfo($"Command received: {command}");

                switch (command)
                {
                    case "LOG":
                        await HandleLogCommand(parts);
                        break;

                    case "GET_LOG":
                        await HandleGetLogCommand(parts, writer);
                        break;

                    default:
                        LogWarning($"Unknown command: {command}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Client error: {ex.Message}");
        }

        LogInfo($"Client {clientEndpoint} disconnected.");
        Console.WriteLine();
    }

    private static async Task HandleLogCommand(string[] parts)
    {
        // Format: LOG|format|json_data or LOG|json_data (legacy)
        string format;
        string jsonData;

        if (parts.Length >= 3)
        {
            // New format: LOG|format|json_data
            format = parts[1].ToLower();
            jsonData = parts[2];
        }
        else if (parts.Length == 2)
        {
            // Legacy format: LOG|json_data
            format = "json";
            jsonData = parts[1];
        }
        else
        {
            LogWarning("Invalid LOG command format");
            return;
        }

        LogEntry? entry;
        try
        {
            entry = JsonSerializer.Deserialize<LogEntry>(jsonData);
        }
        catch (JsonException ex)
        {
            LogError($"Failed to deserialize log entry: {ex.Message}");
            return;
        }

        if (entry == null)
        {
            LogWarning("Received null log entry");
            return;
        }

        _totalLogsReceived++;

        LogSuccess($"LOG received (#{_totalLogsReceived}) - Format: {format.ToUpper()}");
        LogData("Job", entry.JobName);
        LogData("User", entry.UserName);
        LogData("Machine", entry.MachineName);
        LogData("Source", TruncatePath(entry.SourceFile, 50));
        LogData("Target", TruncatePath(entry.TargetFile, 50));
        LogData("Size", FormatFileSize(entry.FileSize));
        LogData("Transfer Time", $"{entry.TransferTimeMs} ms");
        if (entry.EncryptionTimeMs > 0)
            LogData("Encryption Time", $"{entry.EncryptionTimeMs} ms");
        LogData("Timestamp", entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

        await AppendLogAsync(entry, format);
    }

    private static async Task HandleGetLogCommand(string[] parts, StreamWriter writer)
    {
        var format = parts.Length > 1 ? parts[1].ToLower() : "json";

        LogInfo($"GET_LOG request - Format: {format.ToUpper()}");

        var content = await GetLogContentAsync(format);
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        await writer.WriteLineAsync(base64Content);

        var entryCount = CountEntries(content, format);
        LogSuccess($"Sent {entryCount} log entries to client ({format.ToUpper()} format)");
    }

    private static int CountEntries(string content, string format)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        try
        {
            if (format == "xml")
            {
                var serializer = new XmlSerializer(typeof(List<LogEntry>));
                using var reader = new StringReader(content);
                var entries = serializer.Deserialize(reader) as List<LogEntry>;
                return entries?.Count ?? 0;
            }
            else
            {
                var entries = JsonSerializer.Deserialize<List<LogEntry>>(content);
                return entries?.Count ?? 0;
            }
        }
        catch
        {
            return 0;
        }
    }

    private static string TruncatePath(string path, int maxLength)
    {
        if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
            return path;

        return "..." + path.Substring(path.Length - maxLength + 3);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private static string GetTodayLogPath(string format)
    {
        var dir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "CentralLogs");

        Directory.CreateDirectory(dir);

        var extension = format == "xml" ? ".xml" : ".json";

        return Path.Combine(
            dir,
            DateTime.Now.ToString("yyyy-MM-dd") + extension);
    }

    private static async Task AppendLogAsync(LogEntry entry, string format)
    {
        await _fileSemaphore.WaitAsync();

        try
        {
            var path = GetTodayLogPath(format);
            List<LogEntry> entries = new();

            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        if (format == "xml")
                        {
                            var serializer = new XmlSerializer(typeof(List<LogEntry>));
                            using var reader = new StringReader(content);
                            entries = serializer.Deserialize(reader) as List<LogEntry> ?? new();
                        }
                        else
                        {
                            entries = JsonSerializer.Deserialize<List<LogEntry>>(content) ?? new();
                        }
                    }
                    catch
                    {
                        LogWarning($"Could not parse existing log file, starting fresh.");
                        entries = new();
                    }
                }
            }

            entries.Add(entry);

            string output;
            if (format == "xml")
            {
                output = SerializeXml(entries);
            }
            else
            {
                output = JsonSerializer.Serialize(
                    entries,
                    new JsonSerializerOptions { WriteIndented = true });
            }

            await File.WriteAllTextAsync(path, output);

            LogSuccess($"Log appended to {Path.GetFileName(path)} (Total entries today: {entries.Count})");
        }
        catch (Exception ex)
        {
            LogError($"Failed to append log: {ex.Message}");
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private static async Task<string> GetLogContentAsync(string format)
    {
        await _fileSemaphore.WaitAsync();

        try
        {
            var path = GetTodayLogPath(format);

            if (!File.Exists(path))
            {
                LogWarning($"No log file found for today ({format.ToUpper()} format)");
                return string.Empty;
            }

            var content = await File.ReadAllTextAsync(path);

            if (string.IsNullOrWhiteSpace(content))
            {
                LogWarning("Log file is empty");
                return string.Empty;
            }

            return content;
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private static string SerializeXml(List<LogEntry> entries)
    {
        var serializer = new XmlSerializer(typeof(List<LogEntry>));

        using var writer = new StringWriter();
        serializer.Serialize(writer, entries);

        return writer.ToString();
    }
}