using System.Collections.Generic;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class AitDocumentProfileFactory
    {
        public List<AitDocumentProfile> GetProfiles()
        {
            return new List<AitDocumentProfile>
            {
                CreateTechnicalBulletinProfile(),
                CreateUserNoticeProfile(),
                CreateAddendaProfile(),
                CreateReferenceManualProfile(),
                CreateTechnicalDocumentProfile(),
                CreateMultiInstrumentTechnicalDocumentProfile()
            };
        }

        public AitDocumentProfile GetProfile(AitDocumentType documentType)
        {
            foreach (AitDocumentProfile profile in GetProfiles())
            {
                if (profile.DocumentType == documentType)
                {
                    return profile;
                }
            }

            return CreateTechnicalDocumentProfile();
        }

        private AitDocumentProfile CreateTechnicalBulletinProfile()
        {
            return new AitDocumentProfile
            {
                DocumentType = AitDocumentType.TechnicalBulletin,
                DisplayName = "Bulletin Technique",
                PrimaryStylesheet = "Resources/Stylesheets/Styles_BT_Test.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech.flpgl",
                TocPageLayout = "Resources/PageLayouts/TOC_Print.flpgl",
                FrontmatterPageLayout = "Resources/PageLayouts/Frontmatter.flpgl",
                RunActionResultCleanup = true,
                RunBulletListCleanup = true,
                RunCalloutCleanup = true,
                RunFigureCleanup = true,
                RunSimpleStyleCleanup = true,
                RunIhmCleanup = false,
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover",
                    "STA_notice_headers_footers",
                    "PDF_STA_headers et footers"
                }
            };
        }

        private AitDocumentProfile CreateUserNoticeProfile()
        {
            return new AitDocumentProfile
            {
                DocumentType = AitDocumentType.UserNotice,
                DisplayName = "Notice utilisateur",
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Notice_user.flpgl",
                TocPageLayout = "Resources/PageLayouts/TOC_Print.flpgl",
                FrontmatterPageLayout = "Resources/PageLayouts/Frontmatter.flpgl",
                RunActionResultCleanup = true,
                RunBulletListCleanup = true,
                RunCalloutCleanup = true,
                RunFigureCleanup = true,
                RunSimpleStyleCleanup = true,
                RunIhmCleanup = false,
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover"
                }
            };
        }

        private AitDocumentProfile CreateAddendaProfile()
        {
            return new AitDocumentProfile
            {
                DocumentType = AitDocumentType.Addenda,
                DisplayName = "Addenda",
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Addenda.flpgl",
                TocPageLayout = "Resources/PageLayouts/TOC_Print.flpgl",
                FrontmatterPageLayout = "Resources/PageLayouts/Frontmatter.flpgl",
                RunActionResultCleanup = true,
                RunBulletListCleanup = true,
                RunCalloutCleanup = true,
                RunFigureCleanup = true,
                RunSimpleStyleCleanup = true,
                RunIhmCleanup = false,
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover"
                }
            };
        }

        private AitDocumentProfile CreateReferenceManualProfile()
        {
            return new AitDocumentProfile
            {
                DocumentType = AitDocumentType.ReferenceManual,
                DisplayName = "Manuel de référence / MRef",
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Chapters.flpgl",
                TocPageLayout = "Resources/PageLayouts/TOC_Print.flpgl",
                FrontmatterPageLayout = "Resources/PageLayouts/Frontmatter.flpgl",
                RunActionResultCleanup = true,
                RunBulletListCleanup = true,
                RunCalloutCleanup = true,
                RunFigureCleanup = true,
                RunSimpleStyleCleanup = true,
                RunIhmCleanup = false,
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover"
                }
            };
        }

        private AitDocumentProfile CreateTechnicalDocumentProfile()
        {
            return new AitDocumentProfile
            {
                DocumentType = AitDocumentType.TechnicalDocument,
                DisplayName = "Document technique",
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech.flpgl",
                TocPageLayout = "Resources/PageLayouts/TOC_Print.flpgl",
                FrontmatterPageLayout = "Resources/PageLayouts/Frontmatter.flpgl",
                RunActionResultCleanup = true,
                RunBulletListCleanup = true,
                RunCalloutCleanup = true,
                RunFigureCleanup = true,
                RunSimpleStyleCleanup = true,
                RunIhmCleanup = false,
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover"
                }
            };
        }

        private AitDocumentProfile CreateMultiInstrumentTechnicalDocumentProfile()
        {
            return new AitDocumentProfile
            {
                DocumentType = AitDocumentType.MultiInstrumentTechnicalDocument,
                DisplayName = "Document technique multi-instrument",
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech_multi.flpgl",
                TocPageLayout = "Resources/PageLayouts/TOC_Print.flpgl",
                FrontmatterPageLayout = "Resources/PageLayouts/Frontmatter.flpgl",
                RunActionResultCleanup = true,
                RunBulletListCleanup = true,
                RunCalloutCleanup = true,
                RunFigureCleanup = true,
                RunSimpleStyleCleanup = true,
                RunIhmCleanup = false,
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover"
                }
            };
        }
    }
}