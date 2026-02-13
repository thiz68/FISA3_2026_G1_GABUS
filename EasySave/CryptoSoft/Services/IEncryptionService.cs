namespace CryptoSoft.Services;

public interface IEncryptionService
{
    void EncryptFile(string inputFilePath);
    void DecryptFile(string inputFilePath);
}