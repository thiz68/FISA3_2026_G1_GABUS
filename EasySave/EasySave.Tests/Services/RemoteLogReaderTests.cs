using EasySave.Core.Services;
using Moq;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EasySave.Tests.Services;

public class RemoteLogReaderTests
{
    private readonly Mock<Func<string>> _mockGetFormat;
    private readonly RemoteLogReader _reader;

    public RemoteLogReaderTests()
    {
        _mockGetFormat = new Mock<Func<string>>();
        _mockGetFormat.Setup(f => f()).Returns("json");
        _reader = new RemoteLogReader("127.0.0.1", 5000, _mockGetFormat.Object);
    }

    [Fact]
    public async Task ReadCurrentLogAsync_Timeout_ShouldReturnEmpty()
    {
        var result = await _reader.ReadCurrentLogAsync();
        Assert.Empty(result);
    }
}