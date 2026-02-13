using CryptoSoft.Services;
using CryptoSoft.Utils;

namespace CryptoSoft;

internal class Program
{
    static void Main(string[] args)
    {
        IEncryptionService encryptionService = new AesEncryptionService();

        // ============================================
        // MODE EASY SAVE (arguments présents)
        // ============================================
        if (args.Length > 0)
        {
            foreach (var file in args)
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

        // ============================================
        // MODE STANDALONE
        // ============================================
        Console.WriteLine("=== CryptoSoft ===");
        Console.WriteLine("1 - Chiffrer un fichier");
        Console.WriteLine("2 - Déchiffrer un fichier");
        Console.Write("Votre choix : ");

        string? choice = Console.ReadLine();

        Console.Write("Entrez le chemin complet du fichier : ");
        string? filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Aucun fichier spécifié.");
            return;
        }

        try
        {
            switch (choice)
            {
                case "1":
                    encryptionService.EncryptFile(filePath);
                    Console.WriteLine("Chiffrement terminé.");
                    break;

                case "2":
                    encryptionService.DecryptFile(filePath);
                    Console.WriteLine("Déchiffrement terminé.");
                    break;

                default:
                    Console.WriteLine("Choix invalide.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }
}
