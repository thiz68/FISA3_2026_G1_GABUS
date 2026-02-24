/*
 * CompositeLogWriter: decorator that writes to both a local and a remote writer.
 * The local write is always performed first and is considered authoritative.
 * Remote write failures are caught and raised via the static RemoteServerUnreachable
 * event so callers can show a warning without interrupting the backup flow.
 */
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

namespace EasySave.Core.Services;

public class CompositeLogWriter : ILogWriter
{
    private readonly ILogWriter _local;
    private readonly ILogWriter _remote;

    // Raised when the remote server is unreachable; the message is the exception detail.
    public static event EventHandler<string>? RemoteServerUnreachable;

    public CompositeLogWriter(ILogWriter local, ILogWriter remote)
    {
        _local = local;
        _remote = remote;
    }

    public async Task WriteAsync(LogEntry entry, string format = "json")
    {
        // Local write is guaranteed; do it first before attempting the network write.
        await _local.WriteAsync(entry, format);

        try
        {
            await _remote.WriteAsync(entry, format);
        }
        catch (Exception ex)
        {
            RemoteServerUnreachable?.Invoke(this, ex.Message);
        }
    }
}
