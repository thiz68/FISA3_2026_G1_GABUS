using CryptoSoft.Services;

namespace CryptoSoft;

internal class Program
{
    static void Main(string[] args)
    {
        // 🔹 Important pour gérer correctement les caractères spéciaux
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        IEncryptionService encryptionService = new AesEncryptionService();

        // =========================
        // MODE EASY SAVE (arguments)
        // =========================
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

        // =========================
        // MODE STANDALONE
        // =========================
        Console.WriteLine("=== CryptoSoft ===");
        Console.WriteLine("1 - Chiffrer un fichier");
        Console.WriteLine("2 - Déchiffrer un fichier");
        Console.Write("Votre choix : ");

        string? choice = Console.ReadLine();

        Console.Write("Entrez le chemin complet du fichier : ");
        string? filePath = Console.ReadLine();

        try
        {
            switch (choice)
            {
                case "1":
                    encryptionService.EncryptFile(filePath!);
                    Console.WriteLine("Chiffrement terminé.");
                    break;

                case "2":
                    encryptionService.DecryptFile(filePath!);
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

        Console.WriteLine("Appuyez sur une touche pour quitter...");
        Console.ReadKey();
    }
}
