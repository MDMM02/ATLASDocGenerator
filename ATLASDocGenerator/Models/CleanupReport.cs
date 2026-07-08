using System;
using System.Collections.Generic;

namespace ATLASDocGenerator.Models
{

    /// <summary>
    ///  Rapport d'exécution généré pendant le nettoyage AIT
    ///  
    /// Il centralise:
    ///     - Compteurs de fichiers et transformations,
    ///     - les erreurs et avertissements rencontrés,
    ///     - les détails de détection Action/Résulat,
    ///     - les détails de transformation des bullets, figures et styles,
    ///     - Informations liées aux classes IHM et aux v   ri  bles.
    ///     
    /// Modèle utilisé progressivement par les serivces de nettoyage, puis pour afficher ou érire un bilan d'exécution.
    /// 
    /// </summary>
    public class CleanupReport
    {
        public CleanupReport()
        {
            StartedAt = DateTime.Now;

            Errors = new List<string>();
            Warnings = new List<string>();

            ActionResultDetectionDetails = new List<string>();
            BulletListTransformationDetails = new List<string>();
            CalloutTransformationDetails = new List<string>();
            FigureTransformationDetails = new List<string>();
            StyleCleanupDetails = new List<string>();
            IhmVariableMatchingDetails = new List<string>();

            IhmClassOccurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            IhmFilesByClass = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public string ScanRoot { get; set; }

        public int FilesScanned { get; set; }

        public int ActionResultListsTransformed { get; set; }

        public int BulletListsTransformed { get; set; }

        public int CalloutsTransformed { get; set; }

        public int FiguresTransformed { get; set; }

        public int StylesCleaned { get; set; }

        public int IhmItemsDetected { get; set; }

        public string LogFilePath { get; set; }

        public List<string> Errors { get; set; }

        public List<string> Warnings { get; set; }

        public int ActionNumParagraphsDetected { get; set; }

        public int ActionBulletParagraphsDetected { get; set; }

        public int ResultParagraphsDetected { get; set; }

        public List<string> ActionResultDetectionDetails { get; set; }

        public int BulletParagraphsDetected { get; set; }

        public int NoPageBreakBlocksCreated { get; set; }

        public List<string> BulletListTransformationDetails { get; set; }

        public List<string> CalloutTransformationDetails { get; set; }

        public List<string> FigureTransformationDetails { get; set; }

        public List<string> StyleCleanupDetails { get; set; }

        public Dictionary<string, int> IhmClassOccurrences { get; set; }

        public Dictionary<string, HashSet<string>> IhmFilesByClass { get; set; }
        public int IhmVariablesMatched { get; set; }
        public int IhmVariablesMappedToBold { get; set; }
        public List<string> IhmVariableMatchingDetails { get; set; }

        public void AddIhmClassOccurrence(string className, string filePath)
        {
            if (string.IsNullOrWhiteSpace(className))
                return;

            IhmItemsDetected++;

            if (!IhmClassOccurrences.ContainsKey(className))
                IhmClassOccurrences[className] = 0;

            IhmClassOccurrences[className]++;

            if (!IhmFilesByClass.ContainsKey(className))
                IhmFilesByClass[className] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(filePath))
                IhmFilesByClass[className].Add(filePath);
        }
    }
}