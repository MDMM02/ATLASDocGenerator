using System;
using System.IO;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services
{
    public class AtlasDocGenerationService
    {
        public GenerationResult CreateDocumentFolder(DocGenerationRequest request)
        {
            ValidateRequest(request);

            string safeReference = FileNameSanitizer.ToSafeName(request.DocumentReference);
            string safeShortTitle = FileNameSanitizer.ToSafeName(request.ShortTitle);

            string folderName = safeReference + "_" + safeShortTitle;

            string contentFolder = Path.Combine(request.ProjectRoot, "Content");
            string documentFolder = Path.Combine(contentFolder, folderName);

            if (!Directory.Exists(contentFolder))
                throw new Exception("Le dossier Content est introuvable dans le projet sélectionné :\n" + contentFolder);

            if (Directory.Exists(documentFolder))
                throw new Exception("Le dossier documentaire existe déjà :\n" + documentFolder);

            Directory.CreateDirectory(documentFolder);

            GenerationResult result = new GenerationResult();
            result.FolderName = folderName;
            result.DocumentFolderPath = documentFolder;
            result.TocPath = Path.Combine(request.ProjectRoot, "Project", "TOCs", folderName + ".fltoc");
            result.TargetPath = Path.Combine(request.ProjectRoot, "Project", "Targets", folderName + ".fltar");

            TopicDuplicator topicDuplicator = new TopicDuplicator();

            result.CreatedTopicPaths = topicDuplicator.DuplicateTopics(
                request.ProjectRoot,
                documentFolder,
                safeReference,
                request
            );
            TocDuplicator tocDuplicator = new TocDuplicator();

            result.TocPath = tocDuplicator.DuplicateAndUpdateToc(
                request.ProjectRoot,
                folderName,
                safeReference
            );

            TargetDuplicator targetDuplicator = new TargetDuplicator();

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

        private void ValidateRequest(DocGenerationRequest request)
        {
            if (request == null)
                throw new Exception("La demande de génération est vide.");

            if (string.IsNullOrWhiteSpace(request.ProjectRoot))
                throw new Exception("Le dossier du projet est obligatoire.");

            if (!Directory.Exists(request.ProjectRoot))
                throw new Exception("Le dossier du projet n'existe pas :\n" + request.ProjectRoot);

            if (!HasFlareProjectFile(request.ProjectRoot))
                throw new Exception("Le dossier sélectionné ne semble pas être une racine de projet MadCap : aucun fichier .flprj trouvé.");

            if (string.IsNullOrWhiteSpace(request.DocumentReference))
                throw new Exception("La référence du document est obligatoire.");

            if (string.IsNullOrWhiteSpace(request.ShortTitle))
                throw new Exception("Le titre doc abrégé est obligatoire.");

            if (request.ShortTitle.Length > 40)
                throw new Exception("Le titre doc abrégé doit faire 40 caractères maximum.");

            if (string.IsNullOrWhiteSpace(request.Device))
                throw new Exception("Le dispositif est obligatoire.");

            if (string.IsNullOrWhiteSpace(request.Range))
                throw new Exception("La gamme est obligatoire.");

            if (string.IsNullOrWhiteSpace(request.FullTitle))
                throw new Exception("Le titre complet est obligatoire.");
        }

        private bool HasFlareProjectFile(string projectRoot)
        {
            string[] files = Directory.GetFiles(projectRoot, "*.flprj");
            return files != null && files.Length > 0;
        }
    }
}