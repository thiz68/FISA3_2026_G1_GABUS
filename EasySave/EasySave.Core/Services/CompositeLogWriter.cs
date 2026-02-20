using EasySave.Core.Interfaces;
using EasySave.Core.Models;

namespace EasySave.Core.Services;

public class CompositeLogWriter : ILogWriter
{
    private readonly ILogWriter _local;
    private readonly ILogWriter _remote;

    public CompositeLogWriter(ILogWriter local, ILogWriter remote)
    {
        _local = local;
        _remote = remote;
    }

    public async Task WriteAsync(LogEntry entry)
    {
        try
        {
            await _remote.WriteAsync(entry);
        }
        catch
        {
            // Fallback local
        }

        await _local.WriteAsync(entry);
    }
}