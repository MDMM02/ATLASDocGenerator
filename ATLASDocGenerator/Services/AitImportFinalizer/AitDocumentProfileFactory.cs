using System.Collections.Generic;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    /// <summary>
    /// Cette classe crée les profils de configuration utilisés par l'AIT Import Finalizer
    /// Chaque profilcorrespond à un typede document:
    /// - bulletin technique
    /// - notice utilisateur
    /// - addenda
    /// - manuel de référence
    /// ( - doc tech multi instrument)
    /// 
    /// Un profil définit:
    /// - la feuille de style principale
    /// - les layouts de pages à utiliser
    /// - les nettoyages AIT à exécuter
    /// - les entrées de la TOC à supprimer (header, footer)
    /// PS: fonctionnalité IHM désactivée car logique à revoir.
    /// </summary>
    public class AitDocumentProfileFactory
    {
        /// <summary>
        /// Retourne la liste de tous les profils de documents disponibles.
        /// Liste qui alimente le choix du type de document dans l'interface.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Retourne le profil correspondant au type de document demandé
        /// Si aucun profil ne correspond, base => profil document technique par défaut
        /// </summary>
        /// <param name="documentType"></param> Type de document
        /// <returns></returns>
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
                
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech.flpgl",
               

                
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