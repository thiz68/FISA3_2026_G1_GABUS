using EasySave.Core.Interfaces;
using EasySave.Core.Models;

namespace EasySave.Core.Services;

public class CompositeLogWriter : ILogWriter
{
    private readonly ILogWriter _local;
    private readonly ILogWriter _remote;

    /// <summary>
    /// Event raised when remote server is unreachable
    /// </summary>
    public static event EventHandler<string>? RemoteServerUnreachable;

    public CompositeLogWriter(ILogWriter local, ILogWriter remote)
    {
        _local = local;
        _remote = remote;
    }

    public async Task WriteAsync(LogEntry entry, string format = "json")
    {
        // Write to local first (guaranteed to work)
        await _local.WriteAsync(entry, format);

        // Then try remote
        try
        {
            await _remote.WriteAsync(entry, format);
        }
        catch (Exception ex)
        {
            // Notify listeners that remote server is unreachable
            RemoteServerUnreachable?.Invoke(this, ex.Message);
        }
    }
}