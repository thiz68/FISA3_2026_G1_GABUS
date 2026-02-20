using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasySave.Core.Models;

class Program
{
    private static readonly int Port = 5000;
    private static readonly SemaphoreSlim _fileSemaphore = new(1, 1);

    static async Task Main()
    {
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();

        Console.WriteLine($"Log Server started on port {Port}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                var message = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(message))
                    return;

                var parts = message.Split('|', 2);
                var command = parts[0];

                switch (command)
                {
                    case "LOG":
                        if (parts.Length < 2) return;
                        var entry = JsonSerializer.Deserialize<LogEntry>(parts[1]);
                        if (entry != null)
                            await AppendLogAsync(entry);
                        break;

                    case "GET_LOG":
                        var content = await ReadLogFileAsync();
                        await writer.WriteLineAsync(content);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
    }

    private static string GetTodayLogPath()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CentralLogs");
        Directory.CreateDirectory(dir);

        return Path.Combine(dir,
            DateTime.Now.ToString("yyyy-MM-dd") + ".json");
    }

    private static async Task AppendLogAsync(LogEntry entry)
    {
        await _fileSemaphore.WaitAsync();
        try
        {
            var path = GetTodayLogPath();
            List<LogEntry> entries = new();

            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    entries = JsonSerializer.Deserialize<List<LogEntry>>(content)
                              ?? new();
                }
            }

            entries.Add(entry);

            var output = JsonSerializer.Serialize(entries,
                new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(path, output);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private static async Task<string> ReadLogFileAsync()
    {
        await _fileSemaphore.WaitAsync();
        try
        {
            var path = GetTodayLogPath();
            if (!File.Exists(path))
                return string.Empty;

            return await File.ReadAllTextAsync(path);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }
}