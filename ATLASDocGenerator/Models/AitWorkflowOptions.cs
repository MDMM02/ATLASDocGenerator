using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Models
{
    public class AitWorkflowOptions
    {
        public AitDocumentType DocumentType { get; set; }
        public string ProjectRootPath { get; set; }
        public string TocPath { get; set; }
        public string TargetPath { get; set; }
        public bool InstallResources { get; set; }
        public bool CleanContent { get; set; }
        public bool ProcessIhm { get; set; }
        public bool CleanToc { get; set; }
        public bool VerifyTarget { get; set; }
        public bool RepairTarget { get; set; }
        public bool GenerateReport { get; set; }
        public AitCleanupOptions CleanupOptions { get; set; }
    }
}
