using System.Security.Cryptography;
using System.Text;

namespace CryptoSoft.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService()
    {
        // Clé fixe pour POC
        _key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes = AES-256
        _iv = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes
    }

    public void EncryptFile(string inputFilePath)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Fichier introuvable.", inputFilePath);

        string outputFilePath = inputFilePath + ".crypt";

        using FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
        using FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using CryptoStream cryptoStream = new CryptoStream(
            outputFileStream,
            aes.CreateEncryptor(),
            CryptoStreamMode.Write);

        inputFileStream.CopyTo(cryptoStream);

        cryptoStream.FlushFinalBlock();
    }
}