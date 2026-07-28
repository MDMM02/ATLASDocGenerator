namespace ATLASDocGenerator.Models.AitImportFinalizer
{

    /// <summary>
    /// Regroupe les options de configuration pour l'importation AIT
    /// Transmis aux services de finalisation pour savoir quel projet flare traiter, quelle toc et target modifier, variable à renseigner et étapes à exécuter.
    /// </summary>
    public class AitImportFinalizerOptions
    {
        public AitDocumentType DocumentType { get; set; }

        public string ProjectRootPath { get; set; }

        public string TocPath { get; set; }

        public string TargetPath { get; set; }

        public string DocumentTitle { get; set; }

        public string DeviceName { get; set; }

        public string DocumentReference { get; set; }

        public string DocumentIndex { get; set; }

        public string Language { get; set; }

        public string MrefReference { get; set; }

        public bool CopyResources { get; set; }

        public bool CleanToc { get; set; }

        public bool UpdateVariables { get; set; }

        public bool ConfigureTarget { get; set; }
    }
}