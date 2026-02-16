namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;

// Load dictionnary to display correct language
public class LocalizationService : ILocalizationService
{
    // Current language (english default)
    private string _currentLanguage = "en";

    // Event fired when language changes (for MVVM binding updates)
    public event EventHandler? LanguageChanged;

    // Get current language code
    public string CurrentLanguage => _currentLanguage;

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
            ["menu_log_format"] = "7. Change log format",
            ["menu_exit"] = "8. Exit",

            // User input
            ["enter_choice"] = "Enter your choice: ",
            ["enter_name"] = "Enter job name: ",
            ["job_to_remove"] = "Enter job's name or number to remove: ",
            ["job_to_modify"] = "Enter job's name or number to modify: ",
            ["enter_source"] = "Enter source path: ",
            ["enter_target"] = "Enter target path: ",
            ["enter_type"] = "Enter type (1=Full, 2=Differential): ",
            ["press_to_continue"] = "Press any key to continue...",
            ["enter_job_number"] = "Enter job number to execute:",

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
            ["log_format_changed"] = "Log format changed to",

            // Errors
            ["error_max_jobs"] = "Error: Maximum 5 jobs allowed",
            ["error_not_found"] = "Error: Job not found",
            ["invalid_choice"] = "Error: Invalid choice",
            ["job_name_alr_exist"] = "Error : A job with the same name already exists",
            ["error_invalid_name"] = "Error: Job name must have at least 1 character",
            ["error_invalid_source"] = "Error: Source must be an existing directory and cannot be the executable directory",
            ["error_invalid_target"] = "Error: Target must be a valid path",
            ["error_invalid_type"] = "Error: Type is not valid",
            ["error_file_not_found"] = "Warning: File {0} no longer exists, skipping",
            ["input_is_null"] = "Error : Input must contains at least 1 character",
            ["critical_error"] = "Error : A critical error happened, please try again",
            ["backup_failed"] = "Backup failed: an I/O error occurred",

            //Other
            ["source"] = "Source",
            ["target"] = "Target",
            ["type"] = "Type",
            ["full"] = "Full",
            ["diff"] = "Differential",

            // WPF specific
            ["app_title"] = "EasySave 2.0",
            ["dashboard"] = "Dashboard",
            ["backup_jobs"] = "Backup Jobs",
            ["settings"] = "Settings",
            ["exit"] = "Exit",
            ["add_job"] = "Add Job",
            ["execute_all"] = "Execute All",
            ["execute_selected"] = "Execute Selected",
            ["delete"] = "Delete",
            ["edit"] = "Edit",
            ["name"] = "Name",
            ["actions"] = "Actions",
            ["state_file_preview"] = "Real-Time State File (Preview)",
            ["log_file_preview"] = "Daily Log File (Preview)",
            ["state_preview_placeholder"] = "State.json preview will appear here...",
            ["log_preview_placeholder_json"] = "Log.json preview will appear here...",
            ["log_preview_placeholder_xml"] = "Log.xml preview will appear here...",
            ["general_settings"] = "General Settings",
            ["extensions_to_encrypt"] = "Extension(s) to encrypt",
            ["cryptosoft_missing"] = "CryptoSoft software is missing\nWould you like to continue ?",
            ["log_format"] = "Log Format",
            ["save_settings"] = "Save Settings",
            ["settings_saved"] = "Settings saved successfully",
            ["cancel"] = "Cancel",
            ["save"] = "Save",
            ["confirm_delete"] = "Are you sure you want to delete this job?",
            ["confirm_delete_title"] = "Confirm Delete",
            ["no_jobs_selected"] = "No jobs selected",
            ["select_jobs_to_execute"] = "Please select jobs to execute",
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
            ["menu_log_format"] = "7. Changer le format des logs",
            ["menu_exit"] = "8. Quitter",

            // User input
            ["enter_choice"] = "Entrez votre choix: ",
            ["enter_name"] = "Nom du travail: ",
            ["job_to_remove"] = "Entrez le nom ou le numero du travail a supprimer: ",
            ["job_to_modify"] = "Entrez le nom ou le numero du travail a modifier : ",
            ["enter_source"] = "Chemin source: ",
            ["enter_target"] = "Chemin cible: ",
            ["enter_type"] = "Type (1=Complet, 2=Differentiel): ",
            ["press_to_continue"] = "Appuyer sur une touche pour continuer...",
            ["enter_job_number"] = "Entrez les numeros des travaux a executer:",

            // Success
            ["job_created"] = "Travail cree avec succes",
            ["job_removed"] = "Travail supprime avec succes",
            ["job_modified"] = "Travail modifie avec succes",
            ["backup_started"] = "Sauvegarde demarree...",
            ["backup_completed"] = "Sauvegarde terminee",
            ["file_copied"] = "Fichiers copies: {0}",

            // Infos
            ["job_list_empty"] = "Aucun travail cree.",
            ["goodbye"] = "A bientot!",
            ["active"] = "Actif",
            ["inactive"] = "Inactif",
            ["completed"] = "Complete",
            ["failed"] = "Echoue",
            ["log_format_changed"] = "Format des logs changes en",

            // Errors
            ["error_max_jobs"] = "Erreur: Maximum 5 travaux autorises",
            ["error_not_found"] = "Erreur: Travail non trouve",
            ["invalid_choice"] = "Erreur: Choix invalide",
            ["job_name_alr_exist"] = "Erreur : Un travail portant le meme nom existe deja",
            ["error_invalid_name"] = "Erreur: Le nom du travail doit avoir au moins 1 caractere",
            ["error_invalid_source"] = "Erreur: La source doit etre un repertoire existant et ne peut pas etre le repertoire contenant l'executable",
            ["error_invalid_target"] = "Erreur: La cible doit etre un chemin valide",
            ["error_invalid_type"] = "Erreur: Le type entre n'est pas valide",
            ["error_file_not_found"] = "Attention: Le fichier {0} n'existe plus, ignore",
            ["input_is_null"] = "Erreur : Veuillez entrer au moins 1 caractere",
            ["critical_error"] = "Erreur : Une erreur critique est survenue, veuillez reessayer",
            ["backup_failed"] = "Sauvegarde echouee: une erreur d'entree/sortie s'est produite",

            //Other
            ["source"] = "Source",
            ["target"] = "Cible",
            ["type"] = "Type",
            ["full"] = "Complete",
            ["diff"] = "Differentielle",

            // WPF specific
            ["app_title"] = "EasySave 2.0",
            ["dashboard"] = "Tableau de bord",
            ["backup_jobs"] = "Travaux de sauvegarde",
            ["settings"] = "Parametres",
            ["exit"] = "Quitter",
            ["add_job"] = "Ajouter",
            ["execute_all"] = "Executer tout",
            ["execute_selected"] = "Executer selection",
            ["delete"] = "Supprimer",
            ["edit"] = "Modifier",
            ["name"] = "Nom",
            ["actions"] = "Actions",
            ["state_file_preview"] = "Fichier d'etat temps reel (Apercu)",
            ["log_file_preview"] = "Fichier Log journalier (Apercu)",
            ["state_preview_placeholder"] = "L'apercu de l'etat apparaitra ici...",
            ["log_preview_placeholder_json"] = "L'apercu des logs en json apparaitra ici...",
            ["log_preview_placeholder_xml"] = "L'apercu des logs en xml apparaitra ici...",
            ["general_settings"] = "Parametres generaux",
            ["extensions_to_encrypt"] = "Extension(s) à crypter",
            ["cryptosoft_missing"] = "Le logiciel CryptoSoft est manquant\nVoulez-vous continuer sans cryptage ?",
            ["log_format"] = "Format des logs",
            ["save_settings"] = "Enregistrer",
            ["settings_saved"] = "Parametres enregistres avec succes",
            ["cancel"] = "Annuler",
            ["save"] = "Enregistrer",
            ["confirm_delete"] = "Etes-vous sur de vouloir supprimer ce travail ?",
            ["confirm_delete_title"] = "Confirmer la suppression",
            ["no_jobs_selected"] = "Aucun travail selectionne",
            ["select_jobs_to_execute"] = "Veuillez selectionner des travaux a executer",
        }
    };

    // Get text from key
    public string GetString(string key)
    {
        if (_resources[_currentLanguage].TryGetValue(key, out var value))
        {
            return value;
        }
        return key;
    }

    // Change current language
    public void SetLanguage(string languageCode)
    {
        // Check if language id exist before change
        if (_resources.ContainsKey(languageCode))
        {
            _currentLanguage = languageCode;
            // Notify subscribers that language has changed
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
