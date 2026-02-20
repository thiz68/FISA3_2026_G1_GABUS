using System.Net.Sockets;
using System.Text;
using EasySave.Core.Interfaces;

namespace EasySave.Core.Services;

public class RemoteLogReader : ILogReader
{
    private readonly string _serverIp;
    private readonly int _serverPort;
    private readonly Func<string> _getFormat;

    public RemoteLogReader(string serverIp, int serverPort, Func<string> getFormat)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
        _getFormat = getFormat;
    }

    public async Task<string> ReadCurrentLogAsync()
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
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var format = _getFormat();

            await writer.WriteLineAsync($"GET_LOG|{format}");

            var base64Response = await reader.ReadLineAsync() ?? string.Empty;
            var bytes = Convert.FromBase64String(base64Response);

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}