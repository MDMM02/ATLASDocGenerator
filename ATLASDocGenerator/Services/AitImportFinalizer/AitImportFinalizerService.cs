using System;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class AitImportFinalizerService
    {
        private readonly AitDocumentProfileFactory _profileFactory;

        public AitImportFinalizerService()
        {
            _profileFactory = new AitDocumentProfileFactory();
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

            report.Warnings.Add("Phase 1 foundation only: profile selected, no project file modified yet.");
            report.Warnings.Add("Selected stylesheet: " + profile.PrimaryStylesheet);
            report.Warnings.Add("Selected page layout: " + profile.PrimaryPageLayout);

            return report;
        }
    }
}
