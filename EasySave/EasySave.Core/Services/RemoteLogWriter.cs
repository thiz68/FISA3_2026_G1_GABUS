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
        using var client = new TcpClient();

        await client.ConnectAsync(_serverIp, _serverPort);

        using var stream = client.GetStream();

        var json = JsonSerializer.Serialize(entry);
        var data = Encoding.UTF8.GetBytes(json);

        await stream.WriteAsync(data, 0, data.Length);
    }
}