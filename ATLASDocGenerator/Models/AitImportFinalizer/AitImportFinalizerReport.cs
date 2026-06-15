using System.Collections.Generic;

namespace ATLASDocGenerator.Models.AitImportFinalizer
{
    public class AitImportFinalizerReport
    {
        public string ProfileName { get; set; }

        public bool ResourcesCopied { get; set; }

        public bool TocCleaned { get; set; }

        public bool VariablesUpdated { get; set; }

        public bool TargetConfigured { get; set; }

        public bool CleanupLaunched { get; set; }

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