using System.Collections.Generic;

namespace ATLASDocGenerator.Models.AitImportFinalizer
{

    /// <summary>
    ///  Définit le profil de finalisation associé à un type de document AIT.
    ///  Un profil regroupe les paramètres attendus pour transformer un projet importé en projet Flare exploitable.
    ///  Aucun traitement, juste une description de la configuration à appliquer
    /// </summary>
    public class AitDocumentProfile
    {
        public AitDocumentType DocumentType { get; set; }

        public string DisplayName { get; set; }

        public string PrimaryStylesheet { get; set; }

        public string PrimaryPageLayout { get; set; }

        public string TocPageLayout { get; set; }

        public string FrontmatterPageLayout { get; set; }

        public List<string> TocEntriesToRemove { get; set; }

        public bool RunActionResultCleanup { get; set; }

        public bool RunBulletListCleanup { get; set; }

        public bool RunCalloutCleanup { get; set; }

        public bool RunFigureCleanup { get; set; }

        public bool RunSimpleStyleCleanup { get; set; }

        public bool RunIhmCleanup { get; set; }

        public AitDocumentProfile()
        {
            TocEntriesToRemove = new List<string>();
        }
    }
}