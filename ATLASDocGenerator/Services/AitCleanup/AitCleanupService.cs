using System;
using System.Collections.Generic;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services.AitCleanup.IhmVariables;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Orchestre les différentes transformations de nettoyage
    /// appliquées après l'import Author-it dans MadCap Flare.
    /// </summary>
    public class AitCleanupService
    {
        private readonly HtmlFileScanner _scanner;
        private readonly CleanupLogService _logService;

        private readonly ActionResultListTransformer _actionResultTransformer;
        private readonly BulletListTransformer _bulletListTransformer;
        private readonly CalloutTransformer _calloutTransformer;
        private readonly FigureTransformer _figureTransformer;
        private readonly SimpleStyleCleanupTransformer _simpleStyleCleanupTransformer;
       

        /// <summary>
        /// Génère les fichiers .flvar à partir des templates
        /// IHM sélectionnés dans le formulaire.
        /// </summary>
        private readonly FrenchIhmVariableSetGenerator
            _frenchIhmVariableSetGenerator;
        private readonly IhmVariableReferenceTransformer _ihmVariableReferenceTransformer;

        public AitCleanupService()
        {
            _scanner = new HtmlFileScanner();
            _logService = new CleanupLogService();

            _actionResultTransformer =
                new ActionResultListTransformer();

            _bulletListTransformer =
                new BulletListTransformer();

            _calloutTransformer =
                new CalloutTransformer();

            _figureTransformer =
                new FigureTransformer();

            _simpleStyleCleanupTransformer =
                new SimpleStyleCleanupTransformer();

            _frenchIhmVariableSetGenerator =
                new FrenchIhmVariableSetGenerator();

            _ihmVariableReferenceTransformer = new IhmVariableReferenceTransformer();
        }

        /// <summary>
        /// Lance le nettoyage AIT selon les options sélectionnées.
        /// </summary>
        public CleanupReport Run(AitCleanupOptions options)
        {
            CleanupReport report = new CleanupReport();

            try
            {
                if (options == null)
                {
                    throw new ArgumentNullException(
                        "options",
                        "Les options AIT Cleanup sont absentes.");
                }

                /*
                 * ÉTAPE 1
                 * Recherche les fichiers HTML à traiter.
                 */

                string scanRoot;

                List<string> files =
                    _scanner.GetHtmlFiles(
                        options,
                        out scanRoot);

                report.ScanRoot = scanRoot;
                report.FilesScanned = files.Count;

                /*
                 * ÉTAPE 2
                 * Encadrés et figures.
                 */

                if (options.ProcessCallouts)
                {
                    _calloutTransformer.Transform(
                        files,
                        report);
                }

                if (options.ProcessFigures)
                {
                    _figureTransformer.Transform(
                        files,
                        report);
                }

                /*
                 * ÉTAPE 3
                 * Actions, résultats et listes à tirets.
                 */

                if (options.ProcessActionResults)
                {
                    _actionResultTransformer.Transform(
                        files,
                        report);
                }

                if (options.ProcessBulletLists)
                {
                    _bulletListTransformer.Transform(
                        files,
                        report);
                }

                /*
                 * ÉTAPE 4
                 * Nettoyage des styles simples.
                 */

                if (options.ProcessStyleCleanup)
                {
                    _simpleStyleCleanupTransformer.Transform(
                        files,
                        report);
                }

                /*
                 * ÉTAPE 5
                 * Génération des fichiers de variables IHM.
                 */

                if (options.ProcessIhm)
                {
                    GenerateFrenchIhmVariableSets(
                        options,
                        report);
                }

                report.Warnings.Add(
                    "Les transformations sélectionnées ont été exécutées.");
            }
            catch (Exception ex)
            {
                /*
                 * L'erreur est ajoutée au rapport afin que le log
                 * soit quand même généré.
                 */

                report.Errors.Add(
                    ex.Message);
            }
            finally
            {
                report.FinishedAt =
                    DateTime.Now;

                _logService.WriteLog(
                    options,
                    report);
            }

            return report;
        }

        /// <summary>
        /// Génère un fichier .flvar pour chaque template IHM
        /// sélectionné dans la fenêtre AIT Cleanup.
        /// </summary>
        /// <summary>
        /// Génère un fichier .flvar pour chaque template IHM sélectionné,
        /// puis remplace les références de snippets correspondantes
        /// par des références de variables MadCap.
        /// </summary>
        private void GenerateFrenchIhmVariableSets(
     AitCleanupOptions options,
     CleanupReport report)
        {
            if (string.IsNullOrWhiteSpace(
                options.SourceXmlPath))
            {
                throw new InvalidOperationException(
                    "Le fichier XML Author-it source "
                    + "n'a pas été sélectionné.");
            }

            if (options.SelectedIhmTemplateIds == null
                || options.SelectedIhmTemplateIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucun template IHM français "
                    + "n'a été sélectionné.");
            }

            List<FrenchIhmVariableSetGenerationResult> variableResults =
                new List<FrenchIhmVariableSetGenerationResult>();

            /*
             * Génération des fichiers .flvar.
             */

            foreach (string templateId
                in options.SelectedIhmTemplateIds)
            {
                if (string.IsNullOrWhiteSpace(templateId))
                {
                    report.Warnings.Add(
                        "Un ID de template IHM vide a été ignoré.");

                    continue;
                }

                FrenchIhmVariableSetGenerationResult variableResult =
                    _frenchIhmVariableSetGenerator.Generate(
                        options.SourceXmlPath,
                        options.TargetPath,
                        templateId);

                variableResults.Add(
                    variableResult);

                report.IhmVariableSetsGenerated++;

                report.IhmVariablesGenerated +=
                    variableResult.VariablesGenerated;

                report.IhmVariableSetGenerationDetails.Add(
                    variableResult.VariableSetName
                    + ".flvar"
                    + " | Template ID "
                    + variableResult.TemplateId
                    + " | "
                    + variableResult.VariablesGenerated
                    + " variable(s)"
                    + " | "
                    + variableResult.OutputPath);

                foreach (string warning
                    in variableResult.Warnings)
                {
                    report.Warnings.Add(
                        "IHM / "
                        + variableResult.TemplateDescription
                        + " : "
                        + warning);
                }
            }

            if (variableResults.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucun fichier de variables IHM n'a été généré.");
            }

            /*
             * Remplacement des références de snippets.
             */

            IhmVariableReferenceTransformResult transformResult =
                _ihmVariableReferenceTransformer.Transform(
                    options.TargetPath,
                    variableResults);

            report.IhmReferenceFilesScanned =
                transformResult.FilesScanned;

            report.IhmReferenceFilesModified =
                transformResult.FilesModified;

            report.IhmReferencesReplaced =
                transformResult.ReferencesReplaced;

            report.IhmUnmatchedTopicIds =
                transformResult.UnmatchedTopicIds.Count;

            foreach (string detail
                in transformResult.Details)
            {
                report.IhmReferenceReplacementDetails.Add(
                    detail);
            }

            foreach (string error
                in transformResult.Errors)
            {
                report.Errors.Add(
                    "IHM : "
                    + error);
            }
        }
    }
}