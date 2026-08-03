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


        public List<string> TocEntriesToRemove { get; set; }

        public AitDocumentProfile()
        {
            TocEntriesToRemove = new List<string>();
        }
    }
}