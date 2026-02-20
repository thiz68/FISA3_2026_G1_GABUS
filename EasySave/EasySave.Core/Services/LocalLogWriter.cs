using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Text.Json;
using System.Xml.Serialization;

namespace EasySave.Core.Services;

public class LocalLogWriter : ILogWriter
{
    private readonly string _logDirectory;
    private readonly Func<string> _getFormat;
    private static readonly SemaphoreSlim _semaphore = new(1,1);

    public LocalLogWriter(string logDirectory, Func<string> getFormat)
    {
        _logDirectory = logDirectory;
        _getFormat = getFormat;
    }

    public async Task WriteAsync(LogEntry entry)
    {
        await _semaphore.WaitAsync();

        try
        {
            Directory.CreateDirectory(_logDirectory);

            var format = _getFormat();
            var extension = format == "xml" ? ".xml" : ".json";
            var filePath = Path.Combine(_logDirectory,
                DateTime.Now.ToString("yyyy-MM-dd") + extension);

            List<LogEntry> entries = new();

            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    entries = format == "xml"
                        ? DeserializeXml(content)
                        : JsonSerializer.Deserialize<List<LogEntry>>(content) ?? new();
                }
            }

            entries.Add(entry);

            string output = format == "xml"
                ? SerializeXml(entries)
                : JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(filePath, output);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private List<LogEntry> DeserializeXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(List<LogEntry>));
        using var reader = new StringReader(xml);
        return serializer.Deserialize(reader) as List<LogEntry> ?? new();
    }

    private string SerializeXml(List<LogEntry> entries)
    {
        var serializer = new XmlSerializer(typeof(List<LogEntry>));
        using var writer = new StringWriter();
        serializer.Serialize(writer, entries);
        return writer.ToString();
    }
}