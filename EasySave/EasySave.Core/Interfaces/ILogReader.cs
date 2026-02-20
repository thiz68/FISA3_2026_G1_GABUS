namespace EasySave.Core.Interfaces;

public interface ILogReader
{
    Task<string> ReadCurrentLogAsync();
}