using System;
using System.IO;
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
            // Vérifie la présence et la validité des informations utilisateur.
            ValidateRequest(request);

            // Normalise la référence et le titre abrégé afin de produire
            // des noms compatibles avec les règles de nommage du projet.
            string safeReference = FileNameSanitizer.ToSafeName(
                request.DocumentReference
            );

            string safeShortTitle = FileNameSanitizer.ToSafeName(
                request.ShortTitle
            );

            // Le nom du dossier documentaire est composé de la référence et du titre abrégé normalisés.
            string folderName = safeReference + "_" + safeShortTitle;

            // Tous les documents générés sont placés dans le dossier Content du projet.
            string contentFolder = Path.Combine(
                request.ProjectRoot,
                "Content"
            );

            string documentFolder = Path.Combine(
                contentFolder,
                folderName
            );

            if (!Directory.Exists(contentFolder))
            {
                throw new Exception(
                    "Le dossier Content est introuvable dans le projet sélectionné :\n"
                    + contentFolder
                );
            }

            // Empêche d'écraser un dossier documentaire existant.
            if (Directory.Exists(documentFolder))
            {
                throw new Exception(
                    "Le dossier documentaire existe déjà :\n"
                    + documentFolder
                );
            }

            // Création du dossier qui recevra les topics du document.
            Directory.CreateDirectory(documentFolder);

            // Initialise l'objet qui contiendra tous les résultats de la génération.
            GenerationResult result = new GenerationResult
            {
                FolderName = folderName,
                DocumentFolderPath = documentFolder
            };

            // Étape 1 : duplication des topics modèles dans le nouveau dossier.
            TopicDuplicator topicDuplicator = new TopicDuplicator();

            result.CreatedTopicPaths = topicDuplicator.DuplicateTopics(
                request.ProjectRoot,
                documentFolder,
                safeReference,
                request
            );

            // Étape 2 : duplication et adaptation de la TOC modèle.
            TocDuplicator tocDuplicator = new TocDuplicator();

            result.TocPath = tocDuplicator.DuplicateAndUpdateToc(
                request.ProjectRoot,
                folderName,
                safeReference
            );

            // Étape 3 : duplication et adaptation de la target modèle.
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