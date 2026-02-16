using System.Security.Cryptography;
using System.Text;

namespace CryptoSoft.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService()
    {
        _key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
        _iv = Encoding.UTF8.GetBytes("1234567890123456");
    }

    public void EncryptFile(string inputFilePath)
    {
        string normalizedPath = NormalizePath(inputFilePath);

        if (!File.Exists(normalizedPath))
            throw new FileNotFoundException("Fichier introuvable.", normalizedPath);

        string outputFilePath = normalizedPath + ".crypt";

        using (FileStream inputFileStream = new FileStream(normalizedPath, FileMode.Open, FileAccess.Read))
        using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;

            using CryptoStream cryptoStream = new CryptoStream(
                outputFileStream,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write);

            inputFileStream.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
        }

        // Suppression definitive du fichier original
        File.Delete(normalizedPath);
    }

    public void DecryptFile(string inputFilePath)
    {
        string normalizedPath = NormalizePath(inputFilePath);

        if (!File.Exists(normalizedPath))
            throw new FileNotFoundException("Fichier introuvable.", normalizedPath);

        if (!normalizedPath.EndsWith(".crypt", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Le fichier doit avoir l'extension .crypt");

        string outputFilePath = normalizedPath[..^6];

        using (FileStream inputFileStream = new FileStream(normalizedPath, FileMode.Open, FileAccess.Read))
        using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;

            using CryptoStream cryptoStream = new CryptoStream(
                inputFileStream,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read);

            cryptoStream.CopyTo(outputFileStream);
        }

        // Suppression definitive du fichier .crypt
        File.Delete(normalizedPath);
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Chemin vide.");

        path = path.Trim().Trim('"');
        path = Path.GetFullPath(path);

        return path;
    }
}
