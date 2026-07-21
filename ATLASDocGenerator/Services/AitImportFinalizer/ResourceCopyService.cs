using System;
using System.IO;
using System.Reflection;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    /// <summary>
    /// Cette classe copie les ressources nécessaires à la finalisation d'un import AIT ds Flare
    /// 
    /// Les ressources sont stockés dans le dossier Templates déployé à côté de la DLL du plugin:
    /// - Templates/PageLayouts
    /// - Templates/Stylesheets
    /// - Templates/Snippets
    /// - Templates/Images
    /// - Templates/VariableSets
    /// - Templates/Commun Stago
    /// 
    /// </summary>  
    public class ResourceCopyService
    {
        /// <summary>
        /// Copie toutes les ressources du plugin vers le projet Falre
        /// Traitement:
        /// 1. Vérifie que le chemin racine du projet est valide
        /// 2. Localise le dossier Templates situé à côté de la DLL
        /// 3. Copie les layouts, stylesheets, snippets et images (?) ds Content/Ressources
        /// 4. Copie les jeuxde variables ds Project/VariableSets
        /// 5. Copie le dossier Commun Stago directement dsContent/Commun Stago
        /// 6. Vérifie que les ressources principales du profil sont présentes
        /// </summary>
        /// <param name="projectRootPath"></param> Chemin racine du projet Flare
        /// <param name="profile"></param> 
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void CopyResources(string projectRootPath, AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                throw new ArgumentException("Project root path is empty.");
            }

            if (!Directory.Exists(projectRootPath))
            {
                throw new DirectoryNotFoundException("Project root folder not found: " + projectRootPath);
            }
            // Localise le dossier Templates dépployé à côté de la DLL
            string resourceRoot = GetResourceRoot();

            if (!Directory.Exists(resourceRoot))
            {
                throw new DirectoryNotFoundException("AIT resources folder not found: " + resourceRoot);
            }
            // Copie les feuilles de style vers Content/Resources/PageLayouts
            CopyResourceFolder(
                Path.Combine(resourceRoot, "PageLayouts"),
                Path.Combine(projectRootPath, "Content", "Resources", "PageLayouts")
            );
            // Copie les feuilles de style vers Content/Resources/Stylesheets
            CopyResourceFolder(
                Path.Combine(resourceRoot, "Stylesheets"),
                Path.Combine(projectRootPath, "Content", "Resources", "Stylesheets")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "Snippets"),
                Path.Combine(projectRootPath, "Content", "Resources", "Snippets")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "Images"),
                Path.Combine(projectRootPath, "Content", "Resources", "Images")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "VariableSets"),
                Path.Combine(projectRootPath, "Project", "VariableSets")
            );
            // Copie le dossier Commun Stago en conservant son arborescence.
            // Source : Templates/Commun Stago
            // Destination : Content/Commun Stago
            CopyResourceFolder(
                Path.Combine(resourceRoot, "Commun Stago"),
                Path.Combine(projectRootPath, "Content", "Commun Stago")
            );

            // Vérifie que les ressources principales définies dans le profil existent bien dans le projet après la copie.
            ValidateProfileResources(projectRootPath, profile);
        }

        
        // Retourne le chemin du dossier Templates déployé avec le plugin
        private string GetResourceRoot()
        {
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrWhiteSpace(pluginFolder))
            {
                throw new DirectoryNotFoundException("Unable to locate plugin folder.");
            }

            return Path.Combine(pluginFolder, "Templates", "AitResources");
        }
        // Copie récursivemet un dossier et tous ses sous-dossiers
        private void CopyResourceFolder(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                return;
            }
            // Crée le dossier s'il n'existe pas
            Directory.CreateDirectory(destinationFolder);

            foreach (string sourceFilePath in Directory.GetFiles(sourceFolder))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationFolder, fileName);

                File.Copy(sourceFilePath, destinationFilePath, true);
            }

            foreach (string sourceSubFolder in Directory.GetDirectories(sourceFolder))
            {
                string folderName = Path.GetFileName(sourceSubFolder);
                string destinationSubFolder = Path.Combine(destinationFolder, folderName);

                CopyResourceFolder(sourceSubFolder, destinationSubFolder);
            }
        }

        /// <summary>
        /// Vérifie que les ressources principales définies dans le profil existent bien dans le dossier Content du projet après la copie.
        ///
        /// Les chemins enregistrés dans le profil sont relatifs à Content.
        /// Exemple : Resources/Stylesheets/Styles.css.
        /// </summary>
        /// <param name="projectRootPath">Chemin racine du projet Flare.</param>
        /// <param name="profile">Profil du type de document sélectionné.</param>
        private void ValidateProfileResources(string projectRootPath,AitDocumentProfile profile)
        {
            ValidateContentResource(projectRootPath, profile.PrimaryStylesheet, "Feuille de style principale");

            ValidateContentResource(projectRootPath, profile.PrimaryPageLayout, "Layout principal");

            ValidateContentResource(projectRootPath, profile.TocPageLayout, "Layout de la table des matières");

            ValidateContentResource(projectRootPath, profile.FrontmatterPageLayout, "Layout du frontmatter");
        }

        /// <summary>
        /// Vérifie qu'une ressource définie dans le profil existe dans le dossier Content du projet.
        /// </summary>
        /// <param name="projectRootPath">Chemin racine du projet Flare.</param>
        /// <param name="relativePath">Chemin relatif de la ressource depuis Content.</param>
        /// <param name="resourceDescription">Description utilisée dans le message d'erreur.</param>
        private void ValidateContentResource( string projectRootPath, string relativePath, string resourceDescription)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new InvalidOperationException(resourceDescription + " non défini dans le profil du document.");
            }

            // Les chemins des profils utilisent des slashs.
            // On les convertit en séparateurs de dossiers Windows.
            string normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            string fullPath = Path.Combine(projectRootPath, "Content",normalizedRelativePath
            );

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(resourceDescription + " introuvable après la copie : " + fullPath, fullPath);
            }
        }
    }
}