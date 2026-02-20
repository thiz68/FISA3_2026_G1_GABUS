using EasySave.Core.Interfaces;

namespace EasySave.Core.Services;

public class LocalLogReader : ILogReader
{
    private readonly string _logDirectory;
    private readonly Func<string> _getFormat;

    public LocalLogReader(string logDirectory, Func<string> getFormat)
    {
        _logDirectory = logDirectory;
        _getFormat = getFormat;
    }

    public async Task<string> ReadCurrentLogAsync()
    {
        try
        {
            var format = _getFormat();
            var extension = format == "xml" ? ".xml" : ".json";

            var path = Path.Combine(
                _logDirectory,
                DateTime.Now.ToString("yyyy-MM-dd") + extension);

            if (!File.Exists(path))
                return string.Empty;

            return await File.ReadAllTextAsync(path);
        }
        catch
        {
            return string.Empty;
        }
    }
}