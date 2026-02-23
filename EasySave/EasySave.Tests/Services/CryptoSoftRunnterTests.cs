using EasySave.Core.Services;
using System.IO;
using Xunit;

namespace EasySave.Tests.Services;

public class CryptoSoftRunnerTests
{
    private readonly CryptoSoftRunner _runner;

    public CryptoSoftRunnerTests()
    {
        _runner = new CryptoSoftRunner();
    }

    [Fact]
    public void IsCryptoSoftAvailable_NonExistent_ShouldReturnFalse()
    {
        Assert.False(_runner.IsCryptoSoftAvailable());
    }

    [Fact]
    public void EncryptFile_NonExistentExe_ShouldReturnNegative()
    {
        var result = _runner.EncryptFile("test.txt");
        Assert.Equal(-1, result);
    }
}