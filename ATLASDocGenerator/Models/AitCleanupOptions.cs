
namespace ATLASDocGenerator.Models
{
    /// <summary>
    /// Regroupe toutes les options choisies par l'utilisateur
    /// dans la fenêtre AIT Cleanup.
    ///
    /// Cet objet sert uniquement à transmettre la configuration
    /// de nettoyage aux services de traitement.
    /// Il ne doit pas contenir de logique métier.
    /// </summary>
    public class AitCleanupOptions
    {
        /// <summary>
        /// Type de documentation traité.
        /// Exemple : Doc user, Doc tech.
        /// </summary>
        public string DocumentationType { get; set; }

        /// <summary>
        /// Définit le périmètre du nettoyage :
        /// projet complet ou dossier sélectionné uniquement.
        /// </summary>
        public AitCleanupScope Scope { get; set; }

        /// <summary>
        /// Chemin du dossier ou du projet à traiter.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Chemin optionnel vers le XML Author-it source.
        ///
        /// Utilisé pour les traitements IHM / variables lorsque cette phase est activée.
        /// </summary>
        public string SourceXmlPath { get; set; }

        /// <summary>
        /// Active la transformation des paragraphes Action/Résultat.
        /// </summary>
        public bool ProcessActionResults { get; set; }

        /// <summary>
        /// Active la transformation des listes à puces.
        /// </summary>
        public bool ProcessBulletLists { get; set; }

        /// <summary>
        /// Active la transformation des encadrés Information / Précaution / Attention.
        /// </summary>
        public bool ProcessCallouts { get; set; }

        /// <summary>
        /// Active le nettoyage ou la normalisation des figures.
        /// </summary>
        public bool ProcessFigures { get; set; }

        /// <summary>
        /// Active le nettoyage des styles simples importés depuis Author-it.
        /// </summary>
        public bool ProcessStyleCleanup { get; set; }

        /// <summary>
        /// Active le traitement des éléments IHM et variables.
        /// </summary>
        public bool ProcessIhm { get; set; }
    }
}