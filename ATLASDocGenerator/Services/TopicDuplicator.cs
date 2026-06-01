using System;
using System.Collections.Generic;
using System.IO;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services
{
    public class TopicDuplicator
    {
        public List<string> DuplicateTopics(
            string projectRoot,
            string documentFolder,
            string safeReference,
            DocGenerationRequest request)
        {
            List<TopicCopyRule> rules = GetRules(request.DocumentType);
            List<string> createdTopics = new List<string>();

            foreach (TopicCopyRule rule in rules)
            {
                string sourcePath = Path.Combine(projectRoot, rule.SourceRelativePath);
                string targetFileName = rule.TargetFileNamePattern.Replace("{ref}", safeReference);
                string targetPath = Path.Combine(documentFolder, targetFileName);

                if (!File.Exists(sourcePath))
                {
                    throw new Exception(
                        "Topic template introuvable :\n" + sourcePath + "\n\n" +
                        "Vérifie que les templates existent bien dans le projet MadCap de test."
                    );
                }

                if (File.Exists(targetPath))
                {
                    throw new Exception(
                        "Un topic existe déjà avec ce nom :\n" + targetPath
                    );
                }

                File.Copy(sourcePath, targetPath);

                TopicPostProcessor postProcessor = new TopicPostProcessor();
                postProcessor.ProcessCopiedTopic(sourcePath, targetPath);

                createdTopics.Add(targetPath);
            }

            return createdTopics;
        }

        private List<TopicCopyRule> GetRules(string documentType)
        {
            if (documentType == "PS")
                return GetPsRules();

            if (documentType == "Notice")
                return GetNoticeRules();

            throw new Exception("Type de document non reconnu : " + documentType);
        }

        private List<TopicCopyRule> GetPsRules()
        {
            return new List<TopicCopyRule>
            {
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\Title_doc.htm",
                    TargetFileNamePattern = "Title_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Resources\Commun Stago\topics_Tech\Historique_tech.htm",
                    TargetFileNamePattern = "Historique_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\Objectif.htm",
                    TargetFileNamePattern = "Objectif_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\Mesures de sécurité.htm",
                    TargetFileNamePattern = "Mesures_securite_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\Matériel nécessaire.htm",
                    TargetFileNamePattern = "Materiel_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\Documents nécessaires.htm",
                    TargetFileNamePattern = "Documents_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\Prérequis.htm",
                    TargetFileNamePattern = "Prerequis_{ref}.htm"
                },
                new TopicCopyRule
                {
                    SourceRelativePath = @"Content\Template_tech\1er_chapitre.htm",
                    TargetFileNamePattern = "1er_chapitre.htm"
                }
            };
        }

        private List<TopicCopyRule> GetNoticeRules()
        {
            throw new Exception("La génération Notice n'est pas encore configurée. Tester d'abord avec PS.");
        }
    }
}