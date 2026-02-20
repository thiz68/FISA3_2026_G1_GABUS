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
    private const int ConnectionTimeoutMs = 2000;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 100;

    public RemoteLogWriter(string serverIp, int serverPort)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
    }

    public async Task WriteAsync(LogEntry entry, string format = "json")
    {
        for (int retry = 0; retry < MaxRetries; retry++)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(ConnectionTimeoutMs);

                try
                {
                    await client.ConnectAsync(_serverIp, _serverPort, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Connection timeout to log server at {_serverIp}:{_serverPort}");
                }

                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                var json = JsonSerializer.Serialize(entry);
                // Send format along with the log entry: LOG|format|json_data
                await writer.WriteLineAsync($"LOG|{format}|{json}");

                return;
            }
            catch (TimeoutException)
            {
                if (retry == MaxRetries - 1)
                    throw;
            }
            catch (SocketException)
            {
                if (retry == MaxRetries - 1)
                    throw;
            }
            catch (Exception)
            {
                if (retry == MaxRetries - 1)
                    throw;
            }

            await Task.Delay(RetryDelayMs);
        }
    }

    /// <summary>
    /// Check if the server is reachable without sending data
    /// </summary>
    public async Task<bool> IsServerReachableAsync()
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(ConnectionTimeoutMs);

            await client.ConnectAsync(_serverIp, _serverPort, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}