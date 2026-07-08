using System.Collections.Generic;

namespace ATLASDocGenerator.Models
{
    /// <summary>
    /// Résultat retourné après la génération d'un document ATLAS.
    /// Centralise les hemins des élémnts créés:
    ///     dossier documentaire, TOC, target, topics copiés...
    /// Permet au formulaire ou services de savoir ce qui a été créé et où se trouvent les éléments générés.
    /// </summary>
    public class GenerationResult
    {
        public string FolderName { get; set; }
        public string DocumentFolderPath { get; set; }
        public string TocPath { get; set; }
        public string TargetPath { get; set; }

        public List<string> CreatedTopicPaths { get; set; }

        public GenerationResult()
        {
            CreatedTopicPaths = new List<string>();
        }
    }
}