using System.Net.Sockets;
using System.Text;
using EasySave.Core.Interfaces;

namespace EasySave.Core.Services;

public class RemoteLogReader : ILogReader
{
    private readonly string _serverIp;
    private readonly int _serverPort;

    public RemoteLogReader(string serverIp, int serverPort)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
    }

    public async Task<string> ReadCurrentLogAsync()
    {
        try
        {
            using var client = new TcpClient();

            await client.ConnectAsync(_serverIp, _serverPort);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };
            using var reader = new StreamReader(stream, Encoding.UTF8);

            await writer.WriteLineAsync("GET_LOG|");

            return await reader.ReadLineAsync() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}