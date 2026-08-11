using System.Collections.Generic;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitImportFinalizer;

namespace ATLASDocGenerator.Models
{
    public class AitWorkflowReport
    {
        public AitWorkflowReport()
        {
            TocEntriesRemoved = new List<string>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }

        public string ProfileName { get; set; }
        public ResourceCopyResult ResourceCopyResult { get; set; }
        public CleanupReport CleanupReport { get; set; }
        public List<string> TocEntriesRemoved { get; set; }
        public TargetValidationResult TargetValidation { get; set; }
        public bool TargetRepaired { get; set; }
        public string ReportFilePath { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }
    }
}
