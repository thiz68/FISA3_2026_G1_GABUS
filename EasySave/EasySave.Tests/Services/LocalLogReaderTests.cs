using EasySave.Core.Services;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace EasySave.Tests.Services;

public class LocalLogReaderTests
{
    private readonly Mock<Func<string>> _mockGetFormat;
    private readonly LocalLogReader _reader;
    private readonly string _logDir;

    public LocalLogReaderTests()
    {
        _mockGetFormat = new Mock<Func<string>>();
        _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLogs");
        Directory.CreateDirectory(_logDir);
        _reader = new LocalLogReader(_logDir, _mockGetFormat.Object);
    }

    [Fact]
    public async Task ReadCurrentLogAsync_JsonExisting_ShouldReturnContent()
    {
        _mockGetFormat.Setup(f => f()).Returns("json");
        var filePath = Path.Combine(_logDir, DateTime.Now.ToString("yyyy-MM-dd") + ".json");
        await File.WriteAllTextAsync(filePath, "test content");

        var result = await _reader.ReadCurrentLogAsync();
        Assert.Equal("test content", result);
    }
}