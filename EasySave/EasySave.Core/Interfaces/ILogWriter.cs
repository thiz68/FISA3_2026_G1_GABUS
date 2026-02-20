namespace EasySave.Core.Interfaces;

using EasySave.Core.Models;

public interface ILogWriter
{
    Task WriteAsync(LogEntry entry, string format = "json");
}