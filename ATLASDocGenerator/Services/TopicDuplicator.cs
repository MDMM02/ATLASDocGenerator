using System;
using System.Collections.Generic;
using System.IO;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Cette classe duplique les topics modèles nécessaires à la création d'un nouveau document MadCap Flare.
    ///
    /// Selon le type de document sélectionné, elle récupère une liste de règles de copie définissant :
    /// - le chemin du topic modèle
    /// - le nom du topic à créer
    ///
    /// Chaque topic est copié dans le nouveau dossier documentaire, puis traité par TopicPostProcessor afin d'adapter son contenu.
    ///
    /// La génération des documents PS est actuellement configurée.
    /// La génération des Notices reste à implémenter.
    /// </summary>
    public class TopicDuplicator
    {
        /// <summary>
        /// Duplique les topics modèles correspondant au type de document sélectionné.
        ///
        /// Traitement :
        /// 1. Vérifie les paramètres nécessaires
        /// 2. Récupère les règles de copie associées au type de document
        /// 3. Vérifie que chaque topic modèle existe
        /// 4. Copie chaque topic dans le nouveau dossier documentaire
        /// 5. Applique le post-traitement du topic copié
        /// 6. Retourne la liste des topics créés
        /// </summary>
        /// <param name="projectRoot">Chemin racine du projet MadCap Flare.</param>
        /// <param name="documentFolder">Dossier dans lequel les nouveaux topics seront créés.</param>
        /// <param name="safeReference">Référence normalisée utilisée dans les noms de fichiers.</param>
        /// <param name="request">Informations renseignées pour la génération du document.</param>
        /// <returns>Liste des chemins complets des topics créés.</returns>
        public List<string> DuplicateTopics(
            string projectRoot,
            string documentFolder,
            string safeReference,
            DocGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException(
                    "Le chemin racine du projet est vide.",
                    "projectRoot"
                );
            }

            if (!Directory.Exists(projectRoot))
            {
                throw new DirectoryNotFoundException(
                    "Le dossier racine du projet est introuvable :\n"
                    + projectRoot
                );
            }

            if (string.IsNullOrWhiteSpace(documentFolder))
            {
                throw new ArgumentException(
                    "Le chemin du dossier documentaire est vide.",
                    "documentFolder"
                );
            }

            if (!Directory.Exists(documentFolder))
            {
                throw new DirectoryNotFoundException(
                    "Le dossier documentaire est introuvable :\n"
                    + documentFolder
                );
            }

            if (string.IsNullOrWhiteSpace(safeReference))
            {
                throw new ArgumentException(
                    "La référence normalisée du document est vide.",
                    "safeReference"
                );
            }

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            // Récupère les règles de duplication correspondant au type de document sélectionné.
            List<TopicCopyRule> rules = GetRules(
                request.DocumentType
            );

            List<string> createdTopics = new List<string>();

            // Le même post-processeur est utilisé pour tous les topics copiés.
            TopicPostProcessor postProcessor = new TopicPostProcessor();

            foreach (TopicCopyRule rule in rules)
            {
                // Construit le chemin complet du topic modèle.
                string sourcePath = Path.Combine(
                    projectRoot,
                    rule.SourceRelativePath
                );

                // Remplace {ref} dans le nom cible par la référence normalisée du document.
                string targetFileName = rule
                    .TargetFileNamePattern
                    .Replace(
                        "{ref}",
                        safeReference
                    );

                string targetPath = Path.Combine(
                    documentFolder,
                    targetFileName
                );

                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException(
                        "Topic modèle introuvable :\n"
                        + sourcePath
                        + "\n\nVérifie que les templates existent bien "
                        + "dans le projet MadCap sélectionné.",
                        sourcePath
                    );
                }

                // Empêche l'écrasement d'un topic déjà présent dans le dossier documentaire.
                if (File.Exists(targetPath))
                {
                    throw new Exception(
                        "Un topic existe déjà avec ce nom :\n"
                        + targetPath
                    );
                }

                // Copie le topic modèle dans le nouveau dossier documentaire.
                File.Copy(
                    sourcePath,
                    targetPath
                );

                // Applique les adaptations nécessaires au topic copié.
                //
                // Le détail du traitement est géré dans TopicPostProcessor.
                postProcessor.ProcessCopiedTopic(
                    sourcePath,
                    targetPath
                );

                createdTopics.Add(targetPath);
            }

            return createdTopics;
        }

        /// <summary>
        /// Retourne les règles de duplication correspondant au type de document sélectionné.
        ///
        /// Types actuellement reconnus :
        /// - PS
        /// - Notice
        ///
        /// La configuration Notice n'est pas encore implémentée.
        /// </summary>
        /// <param name="documentType">Type de document sélectionné.</param>
        /// <returns>Liste des règles de copie à appliquer.</returns>
        internal List<TopicCopyRule> GetRules(string documentType)
        {
            if (string.IsNullOrWhiteSpace(documentType))
            {
                throw new ArgumentException(
                    "Le type de document est vide.",
                    "documentType"
                );
            }

            if (string.Equals(
                documentType,
                "PS",
                StringComparison.OrdinalIgnoreCase))
            {
                return GetPsRules();
            }

            if (string.Equals(
                documentType,
                "Notice",
                StringComparison.OrdinalIgnoreCase))
            {
                return GetNoticeRules();
            }

            throw new Exception(
                "Type de document non reconnu : "
                + documentType
            );
        }

        /// <summary>
        /// Retourne les règles de duplication utilisées pour générer un document de type PS.
        ///
        /// Chaque règle contient :
        /// - le chemin relatif du topic modèle dans le projet
        /// - le nom du fichier à créer dans le dossier documentaire
        ///
        /// Le marqueur {ref} est remplacé par la référence normalisée pendant la duplication.
        /// </summary>
        /// <returns>Liste des règles utilisées pour les documents PS.</returns>
        private List<TopicCopyRule> GetPsRules()
        {
            return new List<TopicCopyRule>
            {
                new TopicCopyRule
                {
                    // Page de titre du document.
                    SourceRelativePath =
                        @"Content\Template_tech\Title_doc.htm",

                    TargetFileNamePattern =
                        "Title_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Historique des modifications.
                    SourceRelativePath =
                        @"Content\Resources\Commun Stago\topics_Tech\Historique_tech.htm",

                    TargetFileNamePattern =
                        "Historique_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Objectif du document.
                    SourceRelativePath =
                        @"Content\Template_tech\Objectif.htm",

                    TargetFileNamePattern =
                        "Objectif_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Mesures de sécurité.
                    SourceRelativePath =
                        @"Content\Template_tech\Mesures de sécurité.htm",

                    TargetFileNamePattern =
                        "Mesures_securite_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Matériel nécessaire.
                    SourceRelativePath =
                        @"Content\Template_tech\Matériel nécessaire.htm",

                    TargetFileNamePattern =
                        "Materiel_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Documents nécessaires.
                    SourceRelativePath =
                        @"Content\Template_tech\Documents nécessaires.htm",

                    TargetFileNamePattern =
                        "Documents_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Durée d'intervention et remplacements.
                    SourceRelativePath =
                        @"Content\Template_tech\Duree_inter_Remplacements.htm",

                    TargetFileNamePattern =
                        "Duree_inter_Remplacements_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Prérequis nécessaires avant l'intervention.
                    SourceRelativePath =
                        @"Content\Template_tech\Prérequis.htm",

                    TargetFileNamePattern =
                        "Prerequis_{ref}.htm"
                },

                new TopicCopyRule
                {
                    // Premier chapitre générique du nouveau document.
                    SourceRelativePath =
                        @"Content\Template_tech\1er_chapitre.htm",

                    TargetFileNamePattern =
                        "1er_chapitre.htm"
                }
            };
        }

        /// <summary>
        /// Retourne les règles de duplication utilisées pour les Notices.
        ///
        /// Cette fonctionnalité n'est pas encore configurée.
        /// Une exception explicite est donc levée pour éviter de générer un document incomplet ou incorrect.
        /// </summary>
        /// <returns>Liste des règles utilisées pour les Notices.</returns>
        private List<TopicCopyRule> GetNoticeRules()
        {
            throw new Exception(
                "La génération Notice n'est pas encore configurée. "
                + "Tester d'abord avec PS."
            );
        }
    }
}
