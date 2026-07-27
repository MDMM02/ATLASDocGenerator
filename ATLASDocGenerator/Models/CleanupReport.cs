using System;
using System.Collections.Generic;

namespace ATLASDocGenerator.Models
{
    /// <summary>
    /// Rapport d'exécution généré pendant le nettoyage AIT.
    ///
    /// Il centralise :
    /// - les compteurs de fichiers et de transformations ;
    /// - les erreurs et avertissements ;
    /// - les détails des transformations ;
    /// - les résultats de génération et de remplacement des variables IHM.
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

            IhmVariableSetGenerationDetails = new List<string>();
            IhmReferenceReplacementDetails = new List<string>();

            /*
             * Anciennes collections IHM conservées temporairement
             * pour éviter de casser les anciennes classes.
             */
            IhmVariableMatchingDetails = new List<string>();

            IhmClassOccurrences =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            IhmFilesByClass =
                new Dictionary<string, HashSet<string>>(
                    StringComparer.OrdinalIgnoreCase);
        }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public string ScanRoot { get; set; }

        public string LogFilePath { get; set; }

        public List<string> Errors { get; set; }

        public List<string> Warnings { get; set; }

        /*
         * FICHIERS
         */

        public int FilesScanned { get; set; }

        /*
         * ACTIONS / RÉSULTATS
         */

        public int ActionNumParagraphsDetected { get; set; }

        public int ActionBulletParagraphsDetected { get; set; }

        public int ResultParagraphsDetected { get; set; }

        public int ActionResultListsTransformed { get; set; }

        public List<string> ActionResultDetectionDetails { get; set; }

        /*
         * LISTES À TIRETS
         */

        public int BulletListsTransformed { get; set; }

        public int BulletParagraphsDetected { get; set; }

        public int NoPageBreakBlocksCreated { get; set; }

        public List<string> BulletListTransformationDetails { get; set; }

        /*
         * ENCADRÉS
         */

        public int CalloutsTransformed { get; set; }

        public List<string> CalloutTransformationDetails { get; set; }

        /*
         * FIGURES
         */

        public int FiguresTransformed { get; set; }

        public List<string> FigureTransformationDetails { get; set; }

        /*
         * NETTOYAGE DES STYLES
         */

        public int StylesCleaned { get; set; }

        public List<string> StyleCleanupDetails { get; set; }

        /*
         * NOUVEAU TRAITEMENT IHM
         */

        /// <summary>
        /// Nombre de fichiers .flvar générés.
        /// Exemple : Menu_STR.flvar.
        /// </summary>
        public int IhmVariableSetsGenerated { get; set; }

        /// <summary>
        /// Nombre total de variables écrites dans les fichiers .flvar.
        /// </summary>
        public int IhmVariablesGenerated { get; set; }

        /// <summary>
        /// Nombre de fichiers .htm, .html et .flsnp analysés
        /// pour rechercher les références Topic&lt;ID&gt;.flsnp.
        /// </summary>
        public int IhmReferenceFilesScanned { get; set; }

        /// <summary>
        /// Nombre de fichiers dans lesquels au moins une référence
        /// de snippet a été remplacée par une variable.
        /// </summary>
        public int IhmReferenceFilesModified { get; set; }

        /// <summary>
        /// Nombre total de MadCap:snippetText remplacés
        /// par des MadCap:variable.
        /// </summary>
        public int IhmReferencesReplaced { get; set; }

        /// <summary>
        /// Nombre d'IDs Topic rencontrés mais absents du mapping.
        /// </summary>
        public int IhmUnmatchedTopicIds { get; set; }

        /// <summary>
        /// Détails des fichiers de variables générés.
        /// </summary>
        public List<string> IhmVariableSetGenerationDetails { get; set; }

        /// <summary>
        /// Détails des références remplacées.
        /// </summary>
        public List<string> IhmReferenceReplacementDetails { get; set; }

        /*
         * ANCIEN TRAITEMENT IHM
         *
         * Ces propriétés sont conservées temporairement pour que
         * les anciennes classes compilent encore.
         * Elles ne sont plus affichées dans le nouveau log.
         */

        public int IhmItemsDetected { get; set; }

        public Dictionary<string, int> IhmClassOccurrences { get; set; }

        public Dictionary<string, HashSet<string>> IhmFilesByClass { get; set; }

        public int IhmVariablesMatched { get; set; }

        public int IhmVariablesMappedToBold { get; set; }

        public List<string> IhmVariableMatchingDetails { get; set; }

        public void AddIhmClassOccurrence(
            string className,
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(className))
                return;

            IhmItemsDetected++;

            if (!IhmClassOccurrences.ContainsKey(className))
            {
                IhmClassOccurrences[className] = 0;
            }

            IhmClassOccurrences[className]++;

            if (!IhmFilesByClass.ContainsKey(className))
            {
                IhmFilesByClass[className] =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                IhmFilesByClass[className].Add(filePath);
            }
        }
    }
}