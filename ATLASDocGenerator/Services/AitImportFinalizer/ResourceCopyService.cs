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
        private readonly string _resourceRootOverride;

        public ResourceCopyService()
            : this(null)
        {
        }

        public ResourceCopyService(string resourceRootOverride)
        {
            _resourceRootOverride = string.IsNullOrWhiteSpace(resourceRootOverride)
                ? null
                : resourceRootOverride;
        }

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
        public ResourceCopyResult CopyResources(
            string projectRootPath,
            AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                throw new ArgumentException("Project root path is empty.");
            }

            if (!Directory.Exists(projectRootPath))
            {
                throw new DirectoryNotFoundException("Project root folder not found: " + projectRootPath);
            }

            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            ValidateProjectFolder(projectRootPath, "Content");
            ValidateProjectFolder(projectRootPath, "Project");

            // Localise le dossier Templates dépployé à côté de la DLL
            string resourceRoot = GetResourceRoot();

            if (!Directory.Exists(resourceRoot))
            {
                throw new DirectoryNotFoundException("AIT resources folder not found: " + resourceRoot);
            }

            ResourceFolderMapping[] mappings =
            {
                new ResourceFolderMapping(
                    Path.Combine(resourceRoot, "PageLayouts"),
                    Path.Combine(projectRootPath, "Content", "Resources", "PageLayouts")),
                new ResourceFolderMapping(
                    Path.Combine(resourceRoot, "Stylesheets"),
                    Path.Combine(projectRootPath, "Content", "Resources", "Stylesheets")),
                new ResourceFolderMapping(
                    Path.Combine(resourceRoot, "Snippets"),
                    Path.Combine(projectRootPath, "Content", "Resources", "Snippets")),
                new ResourceFolderMapping(
                    Path.Combine(resourceRoot, "Images"),
                    Path.Combine(projectRootPath, "Content", "Resources", "Images")),
                new ResourceFolderMapping(
                    Path.Combine(resourceRoot, "VariableSets"),
                    Path.Combine(projectRootPath, "Project", "VariableSets")),
                new ResourceFolderMapping(
                    Path.Combine(resourceRoot, "Commun Stago"),
                    Path.Combine(projectRootPath, "Content", "Resources", "Commun Stago"))
            };

            // Toutes les sources sont validées avant la première écriture afin
            // d'éviter une copie partielle causée par un package incomplet.
            foreach (ResourceFolderMapping mapping in mappings)
            {
                if (!Directory.Exists(mapping.SourceFolder))
                {
                    throw new DirectoryNotFoundException(
                        "Dossier de ressources ATLAS introuvable : "
                        + mapping.SourceFolder);
                }
            }

            ResourceCopyResult result = new ResourceCopyResult();

            foreach (ResourceFolderMapping mapping in mappings)
            {
                CopyResourceFolder(
                    mapping.SourceFolder,
                    mapping.DestinationFolder,
                    result);
            }

            // Vérifie que les ressources principales définies dans le profil existent bien dans le projet après la copie.
            ValidateProfileResources(projectRootPath, profile);

            return result;
        }

        
        // Retourne le chemin du dossier Templates déployé avec le plugin
        private string GetResourceRoot()
        {
            if (!string.IsNullOrWhiteSpace(_resourceRootOverride))
            {
                return Path.GetFullPath(_resourceRootOverride);
            }

            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrWhiteSpace(pluginFolder))
            {
                throw new DirectoryNotFoundException("Unable to locate plugin folder.");
            }

            return Path.Combine(pluginFolder, "Templates");
        }
        // Copie récursivemet un dossier et tous ses sous-dossiers
        private void CopyResourceFolder(
            string sourceFolder,
            string destinationFolder,
            ResourceCopyResult result)
        {
            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException(
                    "Dossier de ressources ATLAS introuvable : "
                    + sourceFolder);
            }
            // Crée le dossier s'il n'existe pas
            Directory.CreateDirectory(destinationFolder);

            foreach (string sourceFilePath in Directory.GetFiles(sourceFolder))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationFolder, fileName);

                if (!File.Exists(destinationFilePath))
                {
                    File.Copy(sourceFilePath, destinationFilePath, false);
                    result.FilesCopied++;
                    continue;
                }

                // General.flvar appartient au contenu commun. Un fichier déjà
                // présent dans le projet ne doit jamais être écrasé.
                if (IsExistingGeneralVariableSet(destinationFilePath))
                {
                    result.FilesPreserved++;
                    continue;
                }

                if (FilesAreEqual(sourceFilePath, destinationFilePath))
                {
                    result.FilesUnchanged++;
                    continue;
                }

                bool backupCreated = FileBackupService.CreateInitialBackup(
                    destinationFilePath,
                    ".before-ait-finalizer.bak");

                if (backupCreated)
                {
                    result.BackupsCreated++;
                }

                File.Copy(sourceFilePath, destinationFilePath, true);
                result.FilesUpdated++;
            }

            foreach (string sourceSubFolder in Directory.GetDirectories(sourceFolder))
            {
                string folderName = Path.GetFileName(sourceSubFolder);
                string destinationSubFolder = Path.Combine(destinationFolder, folderName);

                CopyResourceFolder(
                    sourceSubFolder,
                    destinationSubFolder,
                    result);
            }
        }

        private bool IsExistingGeneralVariableSet(
            string destinationFilePath)
        {
            if (!Path.GetFileName(destinationFilePath).Equals(
                "General.flvar",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            DirectoryInfo parentFolder =
                Directory.GetParent(destinationFilePath);

            if (parentFolder == null
                || !parentFolder.Name.Equals(
                    "VariableSets",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return File.Exists(destinationFilePath);
        }

        private bool FilesAreEqual(
            string firstPath,
            string secondPath)
        {
            FileInfo first = new FileInfo(firstPath);
            FileInfo second = new FileInfo(secondPath);

            if (first.Length != second.Length)
            {
                return false;
            }

            const int bufferSize = 81920;
            byte[] firstBuffer = new byte[bufferSize];
            byte[] secondBuffer = new byte[bufferSize];

            using (FileStream firstStream = File.OpenRead(firstPath))
            using (FileStream secondStream = File.OpenRead(secondPath))
            {
                int bytesRead;

                while ((bytesRead = firstStream.Read(
                    firstBuffer,
                    0,
                    firstBuffer.Length)) > 0)
                {
                    int secondBytesRead = secondStream.Read(
                        secondBuffer,
                        0,
                        secondBuffer.Length);

                    if (bytesRead != secondBytesRead)
                    {
                        return false;
                    }

                    for (int index = 0; index < bytesRead; index++)
                    {
                        if (firstBuffer[index] != secondBuffer[index])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void ValidateProjectFolder(
            string projectRootPath,
            string folderName)
        {
            string folderPath = Path.Combine(
                projectRootPath,
                folderName);

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(
                    "Le dossier Flare requis est introuvable : "
                    + folderPath);
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

        private class ResourceFolderMapping
        {
            public ResourceFolderMapping(
                string sourceFolder,
                string destinationFolder)
            {
                SourceFolder = sourceFolder;
                DestinationFolder = destinationFolder;
            }

            public string SourceFolder { get; private set; }

            public string DestinationFolder { get; private set; }
        }
    }

    public class ResourceCopyResult
    {
        public int FilesCopied { get; set; }

        public int FilesUpdated { get; set; }

        public int FilesUnchanged { get; set; }

        public int BackupsCreated { get; set; }

        public int FilesPreserved { get; set; }
    }
}
