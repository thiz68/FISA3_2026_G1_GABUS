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
            ["job_modified"] = "Job successfully modified",
            ["menu_modify"] = "3. Modify backup job",
            ["menu_list"] = "4. List backup jobs",
            ["menu_execute"] = "5. Execute backup",
            ["menu_language"] = "6. Change language",
            ["menu_exit"] = "7. Exit",

            // User input
            ["enter_choice"] = "Enter your choice: ",
            ["enter_name"] = "Enter job name: ",
            ["job_to_remove"] = "Enter job's name or number to remove: ",
            ["job_to_modify"] = "Enter job's name or number to modify: ",
            ["enter_source"] = "Enter source path: ",
            ["enter_target"] = "Enter target path: ",
            ["enter_type"] = "Enter type (1=Full, 2=Differential): ",
            ["press_to_continue"] = "Press any key to continue...",
            ["enter_job_number"] = "Enter job numbers to execute:",

            // Success
            ["job_created"] = "Job successfully created",
            ["job_removed"] = "Job successfully removed",
            ["backup_started"] = "Backup started...",
            ["backup_completed"] = "Backup completed",
            ["file_copied"] = "Copied: {0}",

            // Infos
            ["job_list_empty"] = "No backup jobs configured",
            ["goodbye"] = "Goodbye!",
            ["active"] = "Active",
            ["inactive"] = "Inactive",
            ["completed"] = "Completed",
            ["failed"] = "Failed",
            ["backup_failed"] = "Backup failed: an I/O error occurred",

            // Errors
            ["error_max_jobs"] = "Error: Maximum 5 jobs allowed",
            ["error_not_found"] = "Error: Job not found",
            ["invalid_choice"] = "Error: Invalid choice",
            ["job_name_alr_exist"] = "Error : A job with the same name already exists",
            ["error_invalid_name"] = "Error: Job name must have at least 1 character",
            ["error_invalid_source"] = "Error: Source must be an existing directory (not a file)",
            ["error_invalid_target"] = "Error: Target must be a valid path",
            ["error_invalid_type"] = "Error: Type is not valid",
            ["error_file_not_found"] = "Warning: File {0} no longer exists, skipping",
            ["input_is_null"] = "Error : Input must contans at least 1 character",
            ["critical_error"] = "Error : A critical error happened, please try again",

            //Other
            ["source"] = "Source",
            ["target"] = "Target",
            ["type"] = "Type",
            ["full"] = "Full",
            ["diff"] = "Differential",
        },

        // French dict
        ["fr"] = new Dictionary<string, string>
        {
            // Menu
            ["menu_title"] = "=== EasySave v1.0 ===",
            ["menu_create"] = "1. Creer un travail de sauvegarde",
            ["menu_remove"] = "2. Supprimer un travail de sauvegarde",
            ["menu_modify"] = "3. Modifier un travail de sauvegarde",
            ["menu_list"] = "4. Lister les travaux",
            ["menu_execute"] = "5. Executer une sauvegarde",
            ["menu_language"] = "6. Changer la langue",
            ["menu_exit"] = "7. Quitter",

            // User input
            ["enter_choice"] = "Entrez votre choix: ",
            ["enter_name"] = "Nom du travail: ",
            ["job_to_remove"] = "Entrez le nom ou le numéro du travail a supprimer: ",
            ["job_to_modify"] = "Entrez le nom ou le numéro du travail à modifier : ",
            ["enter_source"] = "Chemin source: ",
            ["enter_target"] = "Chemin cible: ",
            ["enter_type"] = "Type (1=Complet, 2=Differentiel): ",
            ["press_to_continue"] = "Appuyer sur une touche pour continuer...",
            ["enter_job_number"] = "Entrez le nombre de travaux a executer:",

            // Success
            ["job_created"] = "Travail cree avec succes",
            ["job_removed"] = "Travail supprime avec succes",
            ["job_modified"] = "Travail modifié avec succès",
            ["backup_started"] = "Sauvegarde demarree...",
            ["backup_completed"] = "Sauvegarde terminee",
            ["file_copied"] = "Fichiers copies: {0}",

            // Infos
            ["job_list_empty"] = "Aucun travail crees.",
            ["goodbye"] = "A bientot!",
            ["active"] = "Actif",
            ["inactive"] = "Inactif",
            ["completed"] = "Complete",
            ["failed"] = "Echoue",
            ["backup_failed"] = "Sauvegarde echouee: une erreur d'entree/sortie s'est produite",

            // Errors
            ["error_max_jobs"] = "Erreur: Maximum 5 travaux autorises",
            ["error_not_found"] = "Erreur: Travail non trouve",
            ["invalid_choice"] = "Erreur: Choix invalide",
            ["job_name_alr_exist"] = "Erreur : Un travail portant le meme nom existe deja",
            ["error_invalid_name"] = "Erreur: Le nom du travail doit avoir au moins 1 caractère",
            ["error_invalid_source"] = "Erreur: La source doit être un répertoire existant (pas un fichier)",
            ["error_invalid_target"] = "Erreur: La cible doit être un chemin valide",
            ["error_invalid_type"] = "Erreur: Le type entré n'est pas valide",
            ["error_file_not_found"] = "Attention: Le fichier {0} n'existe plus, ignoré",
            ["input_is_null"] = "Erreur : Veuillez entrer au moins 1 caractère",
            ["critical_error"] = "Erreur : Une erreur critique est survenue, veuillez reessayer",

            //Other
            ["source"] = "Source",
            ["target"] = "Cible",
            ["type"] = "Type",
            ["full"] = "Complete",
            ["diff"] = "Differentielle",
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
