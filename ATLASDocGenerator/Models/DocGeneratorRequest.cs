namespace ATLASDocGenerator.Models
/// <summary>
/// Représente toute les informations saisies par l'utilisateur dans le formulaire principal de génération.
/// Cet objet sert de "pack de données" transmis aux services:
///     - contient le type de doc, titre, reference, dossier projet Flare, l'appareil concerné...
/// Ne contient pas de logique métier, ni de logique de génération, toutest dans les formulaires et services.
/// 
{
    public class DocGenerationRequest
    {
        public string ProjectRoot { get; set; } // Chemin racine du pojet Flare dans lequel les fichiers seront générés.
        public string DocumentType { get; set; } // Type de document à générer (manuel, guide, etc.)
        public string ShortTitle { get; set; } // Titre court du document, utilisé pour le nom de fichier et le titre dans le document (pas d'accents, d'espaces, caractères spéciaux).
        public string DocumentReference { get; set; } // Référence du document.
        public string Device { get; set; } //Nom de l'instrument ou de l'appareil concerné par le document.
        public string Range { get; set; } // Périmètre ou gamme de l'appareil concerné par le document.
        public string FullTitle { get; set; } // Titre complet, destiné à être affiché dans le document généré (peut contenir des accents, espaces, caractères spéciaux).
    }
}