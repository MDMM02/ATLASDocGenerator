using System.Collections.Generic;

namespace ATLASDocGenerator.Models.AitImportFinalizer
{
    /// <summary>
    ///  Rapport d'exécution de l'AIT Import Finalizer.
    ///   Indique quelles étapes ont été exécutées avec succès et conserve les informations utiles pour afficher un bilan d'exécution:
    ///     - profil utilisé ;
    ///     - ressources copiées ;
    ///     - TOC nettoyé ;
    ///     - variables mises à jour ;
    ///     - target configuré ;
    ///     - avertissements et erreurs.
    ///     
    /// </summary>
    public class AitImportFinalizerReport
    {
        public string ProfileName { get; set; }

        public bool ResourcesCopied { get; set; }

        public bool TocCleaned { get; set; }

        public bool VariablesUpdated { get; set; }

        public bool TargetConfigured { get; set; }

        public List<string> TocEntriesRemoved { get; set; }

        public List<string> Warnings { get; set; }

        public List<string> Errors { get; set; }

        public AitImportFinalizerReport()
        {
            TocEntriesRemoved = new List<string>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }
    }
}