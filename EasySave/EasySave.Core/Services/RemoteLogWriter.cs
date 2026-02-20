using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EasySave.Core.Services;

public class RemoteLogWriter : ILogWriter
{
    private readonly string _serverIp;
    private readonly int _serverPort;

    public RemoteLogWriter(string serverIp, int serverPort)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
    }

    public async Task WriteAsync(LogEntry entry)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 100;

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                using var client = new TcpClient();

                var connectTask = client.ConnectAsync(_serverIp, _serverPort);
                var completedTask = await Task.WhenAny(
                    connectTask,
                    Task.Delay(3000));

                if (completedTask != connectTask)
                    throw new TimeoutException("Connection timeout to log server");

                if (connectTask.Exception != null)
                    throw connectTask.Exception.InnerException ?? connectTask.Exception;

                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                var json = JsonSerializer.Serialize(entry);
                await writer.WriteLineAsync($"LOG|{json}");

                return;
            }
            catch
            {
                if (retry == maxRetries - 1)
                    throw;

                await Task.Delay(retryDelayMs);
            }
        }
    }
}