using System.Diagnostics;

namespace EasySave.Core.Services;

public class CryptoSoftRunner
{
    private const string CryptoSoftExeName = "CryptoSoft.exe";

    public bool IsCryptoSoftAvailable()
    {
        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CryptoSoftExeName);
        return File.Exists(exePath);
    }

    public long EncryptFile(string filePath)
    {
        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CryptoSoftExeName);

        if (!File.Exists(exePath))
            return -1;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            stopwatch.Stop();

            if (process.ExitCode == 0)
                return stopwatch.ElapsedMilliseconds;

            return -stopwatch.ElapsedMilliseconds;
        }
        catch
        {
            stopwatch.Stop();
            return -stopwatch.ElapsedMilliseconds;
        }
    }
}