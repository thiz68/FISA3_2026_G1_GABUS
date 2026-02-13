using CryptoSoft.Services;
using CryptoSoft.Utils;

namespace CryptoSoft;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== CryptoSoft ===");

        IEncryptionService encryptionService = new AesEncryptionService();

        // Mode 1 : appelé par EasySave
        if (args.Length > 0)
        {
            var files = ArgumentParser.Parse(args);

            foreach (var file in files)
            {
                try
                {
                    encryptionService.EncryptFile(file);
                    Console.WriteLine($"Fichier chiffré : {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur pour {file} : {ex.Message}");
                }
            }

            return;
        }

        // Mode 2 : standalone
        Console.WriteLine("Entrez le chemin complet du fichier à crypter : ");
        string? filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Aucun fichier spécifié.");
            return;
        }

        try
        {
            encryptionService.EncryptFile(filePath);
            Console.WriteLine("Chiffrement terminé.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }
}