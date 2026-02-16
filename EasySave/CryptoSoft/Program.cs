using CryptoSoft.Services;

namespace CryptoSoft;

internal class Program
{
    static void Main(string[] args)
    {
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
                    Console.WriteLine($"Fichier chiffre et original supprime : {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur pour {file} : {ex.Message}");
                }
            }

            return;
        }

        // =========================
        // MODE STANDALONE (boucle)
        // =========================
        while (true)
        {
            Console.Clear();

            Console.WriteLine("=== CryptoSoft ===");
            Console.WriteLine("1 - Chiffrer un fichier");
            Console.WriteLine("2 - Dechiffrer un fichier");
            Console.WriteLine("0 - Quitter");
            Console.Write("Votre choix : ");

            string? choice = Console.ReadLine();

            if (choice == "0")
                return;

            Console.Write("Entrez le chemin complet du fichier : ");
            string? filePath = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        encryptionService.EncryptFile(filePath!);
                        Console.WriteLine("\nChiffrement termine. Fichier original supprime.");
                        break;

                    case "2":
                        encryptionService.DecryptFile(filePath!);
                        Console.WriteLine("\nDechiffrement termine. Fichier .crypt supprime.");
                        break;

                    default:
                        Console.WriteLine("\nChoix invalide.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErreur : {ex.Message}");
            }

            Console.WriteLine("\nAppuyez sur une touche pour revenir au menu...");
            Console.ReadKey();
        }
    }
}
