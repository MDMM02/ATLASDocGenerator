using System;
using System.Collections.Generic;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class AitImportFinalizerService
    {
        private readonly AitDocumentProfileFactory _profileFactory;
        private readonly TocCleanerService _tocCleanerService;

        public AitImportFinalizerService()
        {
            _profileFactory = new AitDocumentProfileFactory();
            _tocCleanerService = new TocCleanerService();
        }

        public AitImportFinalizerReport Run(AitImportFinalizerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            AitDocumentProfile profile = _profileFactory.GetProfile(options.DocumentType);

            AitImportFinalizerReport report = new AitImportFinalizerReport
            {
                ProfileName = profile.DisplayName
            };

            report.Warnings.Add("Selected stylesheet: " + profile.PrimaryStylesheet);
            report.Warnings.Add("Selected page layout: " + profile.PrimaryPageLayout);

            if (options.CleanToc)
            {
                try
                {
                    List<string> removedEntries = _tocCleanerService.CleanToc(options.TocPath, profile);

                    report.TocCleaned = true;
                    report.TocEntriesRemoved.AddRange(removedEntries);

                    if (removedEntries.Count == 0)
                    {
                        report.Warnings.Add("TOC cleanup completed, but no matching parasite entry was found.");
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("TOC cleanup failed: " + ex.Message);
                }
            }

            return report;
        }
    }
}