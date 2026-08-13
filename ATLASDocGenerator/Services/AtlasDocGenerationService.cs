using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Cette classe orchestre la création d'un nouveau dossier documentaire dans un projet MadCap Flare.
    ///
    /// Elle réalise les principales étapes du Doc Generator :
    /// - validation des informations saisies par l'utilisateur
    /// - création du nom normalisé du document
    /// - création du dossier documentaire dans Content
    /// - duplication et adaptation des topics modèles
    /// - duplication et adaptation de la TOC
    /// - duplication et adaptation de la target
    ///
    /// La classe retourne un GenerationResult contenant les chemins de tous les éléments créés.
    /// </summary>
    public class AtlasDocGenerationService
    {
        /// <summary>
        /// Crée la structure complète d'un nouveau document dans le projet Flare.
        ///
        /// Traitement :
        /// 1. Vérifie les informations de la demande
        /// 2. Normalise la référence et le titre abrégé
        /// 3. Crée le dossier documentaire dans Content
        /// 4. Duplique les topics modèles
        /// 5. Duplique et configure la TOC
        /// 6. Duplique et configure la target
        /// 7. Retourne le résultat de la génération
        /// </summary>
        /// <param name="request">
        /// Informations renseignées par l'utilisateur pour créer le document.
        /// </param>
        /// <returns>
        /// Résultat contenant le nom du dossier et les chemins des fichiers créés.
        /// </returns>
        public GenerationResult CreateDocumentFolder(DocGenerationRequest request)
        {
            ValidateRequest(request);

            string safeReference = FileNameSanitizer.ToSafeName(
                request.DocumentReference
            );
            string safeShortTitle = FileNameSanitizer.ToSafeName(
                request.ShortTitle
            );
            string folderName = safeReference + "_" + safeShortTitle;
            string contentFolder = Path.Combine(request.ProjectRoot, "Content");
            string documentFolder = Path.Combine(contentFolder, folderName);
            string tocPath = Path.Combine(
                request.ProjectRoot,
                "Project",
                "TOCs",
                folderName + ".fltoc"
            );
            string targetPath = Path.Combine(
                request.ProjectRoot,
                "Project",
                "Targets",
                folderName + ".fltar"
            );

            if (!Directory.Exists(contentFolder))
            {
                throw new DirectoryNotFoundException(
                    "Le dossier Content est introuvable dans le projet sélectionné :\n"
                    + contentFolder
                );
            }

            if (Directory.Exists(documentFolder))
            {
                throw new IOException(
                    "Le dossier documentaire existe déjà :\n" + documentFolder
                );
            }

            TopicDuplicator topicDuplicator = new TopicDuplicator();
            TocDuplicator tocDuplicator = new TocDuplicator();
            TargetDuplicator targetDuplicator = new TargetDuplicator();

            // Valide toutes les entrées avant la première écriture afin qu'une
            // erreur de modèle ne laisse pas un document partiellement créé.
            ValidateProjectPrerequisites(
                request,
                topicDuplicator,
                tocDuplicator,
                targetDuplicator,
                tocPath,
                targetPath
            );

            try
            {
                Directory.CreateDirectory(documentFolder);

                GenerationResult result = new GenerationResult
                {
                    FolderName = folderName,
                    DocumentFolderPath = documentFolder
                };

                result.CreatedTopicPaths = topicDuplicator.DuplicateTopics(
                    request.ProjectRoot,
                    documentFolder,
                    safeReference,
                    request
                );
                result.TocPath = tocDuplicator.DuplicateAndUpdateToc(
                    request.ProjectRoot,
                    folderName,
                    safeReference,
                    request.DocumentType
                );
                result.TargetPath = targetDuplicator.DuplicateAndUpdateTarget(
                    request.ProjectRoot,
                    folderName,
                    safeReference,
                    request.Range,
                    request.Device,
                    request.FullTitle
                );

                return result;
            }
            catch (Exception ex)
            {
                TryRollbackGeneratedArtifacts(
                    documentFolder,
                    tocPath,
                    targetPath,
                    ex
                );
                throw;
            }
        }

        private void ValidateProjectPrerequisites(
            DocGenerationRequest request,
            TopicDuplicator topicDuplicator,
            TocDuplicator tocDuplicator,
            TargetDuplicator targetDuplicator,
            string targetTocPath,
            string targetTargetPath)
        {
            if (File.Exists(targetTocPath))
            {
                throw new IOException(
                    "Une TOC existe déjà avec ce nom :\n" + targetTocPath
                );
            }

            if (File.Exists(targetTargetPath))
            {
                throw new IOException(
                    "Une target existe déjà avec ce nom :\n" + targetTargetPath
                );
            }

            List<TopicCopyRule> rules = topicDuplicator.GetRules(
                request.DocumentType
            );
            foreach (TopicCopyRule rule in rules)
            {
                string topicPath = topicDuplicator.ResolveSourceTopicPath(
                    request.ProjectRoot,
                    rule);
                RequireFile(topicPath, "Topic modèle");
                ValidateXmlFile(topicPath, "topic modèle");
            }

            string sourceTocDescription;
            XDocument sourceToc = tocDuplicator.LoadSourceToc(
                request.ProjectRoot,
                request.DocumentType,
                out sourceTocDescription
            );
            ValidateTocLinks(
                request.ProjectRoot,
                sourceToc,
                sourceTocDescription,
                tocDuplicator
            );

            string sourceTargetDescription;
            targetDuplicator.LoadSourceTarget(
                request.ProjectRoot,
                out sourceTargetDescription
            );

            string stylesheetRelativePath = targetDuplicator
                .GetStylesheetPath(request.Range)
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            RequireFile(
                Path.Combine(request.ProjectRoot, stylesheetRelativePath),
                "Feuille de style"
            );
            RequireFile(
                Path.Combine(
                    request.ProjectRoot,
                    "Content",
                    "Resources",
                    "PageLayouts",
                    "Tech.flpgl"),
                "Mise en page Tech"
            );
            RequireFile(
                Path.Combine(
                    request.ProjectRoot,
                    "Project",
                    "VariableSets",
                    "General.flvar"),
                "Jeu de variables General"
            );
        }

        private XDocument ValidateXmlFile(string path, string description)
        {
            try
            {
                return XDocument.Load(path, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    "Le " + description + " n'est pas un XML valide :\n"
                    + path
                    + "\n\nDétail : "
                    + ex.Message,
                    ex
                );
            }
        }

        private void ValidateTocLinks(
            string projectRoot,
            XDocument sourceToc,
            string sourceTocPath,
            TocDuplicator tocDuplicator)
        {
            IEnumerable<string> contentLinks = sourceToc
                .Descendants()
                .SelectMany(element => element.Attributes())
                .Where(attribute => attribute.Name.LocalName.Equals(
                    "Link",
                    StringComparison.OrdinalIgnoreCase))
                .Select(attribute => attribute.Value)
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Where(link => link.Replace('\\', '/').StartsWith(
                    "/Content/",
                    StringComparison.OrdinalIgnoreCase));

            foreach (string link in contentLinks)
            {
                string normalizedLink = link.Replace('\\', '/');
                int fragmentIndex = normalizedLink.IndexOfAny(
                    new[] { '#', '?' }
                );
                if (fragmentIndex >= 0)
                {
                    normalizedLink = normalizedLink.Substring(0, fragmentIndex);
                }

                // Les liens de la TOC modèle vers les anciens topics Template_tech
                // sont remplacés par les topics créés dans le nouveau document.
                // Leurs sources ont déjà été validées via TopicDuplicator, qui
                // prend aussi en charge le nouvel emplacement Topics_Tech.
                if (tocDuplicator.IsGeneratedTopicTemplateLink(normalizedLink))
                {
                    continue;
                }

                string relativePath = Uri.UnescapeDataString(
                    normalizedLink.TrimStart('/')
                ).Replace('/', Path.DirectorySeparatorChar);
                string linkedPath = Path.Combine(projectRoot, relativePath);

                if (!File.Exists(linkedPath))
                {
                    throw new FileNotFoundException(
                        "La TOC modèle référence un fichier introuvable :\n"
                        + linkedPath
                        + "\n\nTOC : "
                        + sourceTocPath,
                        linkedPath
                    );
                }
            }
        }

        private void RequireFile(string path, string description)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    description + " introuvable :\n" + path,
                    path
                );
            }
        }

        private void TryRollbackGeneratedArtifacts(
            string documentFolder,
            string tocPath,
            string targetPath,
            Exception originalException)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                if (File.Exists(tocPath))
                {
                    File.Delete(tocPath);
                }
                if (Directory.Exists(documentFolder))
                {
                    Directory.Delete(documentFolder, true);
                }
            }
            catch (Exception rollbackException)
            {
                originalException.Data["RollbackError"] =
                    rollbackException.Message;
            }
        }

        /// <summary>
        /// Vérifie que toutes les informations obligatoires sont présentes avant de lancer la génération du document.
        ///
        /// Les contrôles portent notamment sur :
        /// - le chemin du projet
        /// - la présence d'un fichier projet MadCap .flprj
        /// - la référence du document
        /// - le titre abrégé
        /// - la limite de 40 caractères du titre abrégé
        /// - le dispositif
        /// - la gamme
        /// - le titre complet
        /// </summary>
        /// <param name="request">Demande de génération à vérifier.</param>
        private void ValidateRequest(DocGenerationRequest request)
        {
            if (request == null)
            {
                throw new Exception(
                    "La demande de génération est vide."
                );
            }

            if (string.IsNullOrWhiteSpace(request.ProjectRoot))
            {
                throw new Exception(
                    "Le dossier du projet est obligatoire."
                );
            }

            if (!Directory.Exists(request.ProjectRoot))
            {
                throw new Exception(
                    "Le dossier du projet n'existe pas :\n"
                    + request.ProjectRoot
                );
            }

            // Vérifie que le dossier sélectionné semble bien être
            // la racine d'un projet MadCap Flare.
            if (!HasFlareProjectFile(request.ProjectRoot))
            {
                throw new Exception(
                    "Le dossier sélectionné ne semble pas être une racine "
                    + "de projet MadCap : aucun fichier .flprj trouvé."
                );
            }

            if (string.IsNullOrWhiteSpace(request.DocumentReference))
            {
                throw new Exception(
                    "La référence du document est obligatoire."
                );
            }

            if (string.IsNullOrWhiteSpace(request.ShortTitle))
            {
                throw new Exception(
                    "Le titre doc abrégé est obligatoire."
                );
            }

            // Le titre abrégé est limité à 40 caractères conformément aux règles du Doc Generator.
            if (request.ShortTitle.Length > 40)
            {
                throw new Exception(
                    "Le titre doc abrégé doit faire 40 caractères maximum."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Device))
            {
                throw new Exception(
                    "Le dispositif est obligatoire."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Range))
            {
                throw new Exception(
                    "La gamme est obligatoire."
                );
            }

            if (string.IsNullOrWhiteSpace(request.FullTitle))
            {
                throw new Exception(
                    "Le titre complet est obligatoire."
                );
            }
        }

        /// <summary>
        /// Vérifie si le dossier sélectionné contient au moins un fichier projet MadCap Flare avec l'extension .flprj.
        /// </summary>
        /// <param name="projectRoot">Chemin du dossier racine à vérifier.</param>
        /// <returns>
        /// True si un fichier .flprj est présent, sinon false.
        /// </returns>
        private bool HasFlareProjectFile(string projectRoot)
        {
            string[] files = Directory.GetFiles(
                projectRoot,
                "*.flprj",
                SearchOption.TopDirectoryOnly
            );

            return files.Length > 0;
        }
    }
}
