namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;

// Load dictionnary to display correct language
public class LocalizationService : ILocalizationService
{
    // Current language (english default)
    private string _currentLanguage = "en";

    // Dictionnary : Key and associated text
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
    {
        // English dict
        ["en"] = new Dictionary<string, string>
        {
            // Menu 
            ["menu_title"] = "=== EasySave v1.0 ===",
            ["menu_create"] = "1. Create backup job",
            ["menu_remove"] = "2. Remove backup job",
            ["menu_list"] = "3. List backup jobs",
            ["menu_execute"] = "4. Execute backup",
            ["menu_language"] = "5. Change language",
            ["menu_exit"] = "6. Exit",

            // User input
            ["enter_choice"] = "Enter your choice: ",
            ["enter_name"] = "Enter job name: ",
            ["job_number_remove"] = "Enter job number to remove: ",
            ["enter_source"] = "Enter source path: ",
            ["enter_target"] = "Enter target path: ",
            ["enter_type"] = "Enter type (1=Full, 2=Differential): ",

            // Success
            ["job_created"] = "Job successfully created",
            ["job_removed"] = "Job successfully removed",
            ["backup_started"] = "Backup started...",
            ["backup_completed"] = "Backup completed",
            ["file_copied"] = "Copied: {0}",

            // Infos
            ["job_list_empty"] = "No backup jobs configured",
            ["goodbye"] = "Goodbye!",

            // Errors
            ["error_max_jobs"] = "Error: Maximum 5 jobs allowed",
            ["error_not_found"] = "Error: Job not found"
        },

        // French dict
        ["fr"] = new Dictionary<string, string>
        {
            // Menu
            ["menu_title"] = "=== EasySave v1.0 ===",
            ["menu_create"] = "1. Creer un travail de sauvegarde",
            ["menu_remove"] = "2. Supprimer un travail de sauvegarde",
            ["menu_list"] = "3. Lister les travaux",
            ["menu_execute"] = "4. Executer une sauvegarde",
            ["menu_language"] = "5. Changer la langue",
            ["menu_exit"] = "6. Quitter",

            // User input
            ["enter_choice"] = "Entrez votre choix: ",
            ["enter_name"] = "Nom du travail: ",
            ["job_number_remove"] = "Entrez le numero du travail a supprimer : ",
            ["enter_source"] = "Chemin source: ",
            ["enter_target"] = "Chemin cible: ",
            ["enter_type"] = "Type (1=Complet, 2=Differentiel): ",

            // Success
            ["job_created"] = "Travail cree avec succes",
            ["job_removed"] = "Travail supprime avec succes",
            ["backup_started"] = "Sauvegarde demarree...",
            ["backup_completed"] = "Sauvegarde terminee",
            ["file_copied"] = "Fichiers copies: {0}",

            // Infos
            ["job_list_empty"] = "Aucun travail crees.",
            ["goodbye"] = "A bientot!",

            // Errors
            ["error_max_jobs"] = "Erreur: Maximum 5 travaux autorises",
            ["error_not_found"] = "Erreur: Travail non trouve"
        }
    };

    // Get text from key
    public string GetString(string key)
    {
        if (_resources[_currentLanguage][key] != null)
        {
            return _resources[_currentLanguage][key];
        }

        else
        {
            return key;
        }
    }

    // Change current language
    public void SetLanguage(string languageCode)
    {
        // Check if language id exist before change
        if (_resources.ContainsKey(languageCode))
            _currentLanguage = languageCode;
    }
}
