using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasySave.Core.Models;

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();

Console.WriteLine("Log Server started...");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClient(client));
}

static async Task HandleClient(TcpClient client)
{
    using var stream = client.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8);

    var json = await reader.ReadToEndAsync();

    var entry = JsonSerializer.Deserialize<LogEntry>(json);

    if (entry == null) return;

    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CentralLogs");
    Directory.CreateDirectory(logDir);

    var path = Path.Combine(logDir,
        DateTime.Now.ToString("yyyy-MM-dd") + ".json");

    var semaphore = new SemaphoreSlim(1,1);
    await semaphore.WaitAsync();

    try
    {
        List<LogEntry> entries = new();

        if (File.Exists(path))
        {
            var content = await File.ReadAllTextAsync(path);
            if (!string.IsNullOrWhiteSpace(content))
                entries = JsonSerializer.Deserialize<List<LogEntry>>(content) ?? new();
        }

        entries.Add(entry);

        var output = JsonSerializer.Serialize(entries,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(path, output);
    }
    finally
    {
        semaphore.Release();
    }
}