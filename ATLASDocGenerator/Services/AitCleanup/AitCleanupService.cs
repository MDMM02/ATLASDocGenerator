using System;
using System.Collections.Generic;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe orchestre les différentes transformations de nettoyage AIT sur les fichiers HTML. 
    /// Elle utilise services et transformateurs pour effectuer des opérations  telles que la transformation des figures, des listes d'actions, des listes à puces, des callouts, et le nettoyage de styles simples.
    /// Elle gère aussi l'analyse des diagnostics IHM et la génération de fichiers JSON pour les variables IHM ( à revoir)
    /// </summary>
    public class AitCleanupService
    {
        private readonly HtmlFileScanner _scanner; // Récupérer la liste des fichiers .htm et .html à traiter
        private readonly CleanupLogService _logService; // Chargé d'écrire le rapport d'exécution sur disque
        private readonly ActionResultListDetector _actionResultDetector; // Détecte les listes d'actions dans les fichiers HTML
        private readonly ActionResultListTransformer _actionResultTransformer; // Transforme les listes d'actions détectées en un format standardisé
        private readonly BulletListTransformer _bulletListTransformer; // Transforme les listes à puces dans les fichiers HTML
        private readonly CalloutTransformer _calloutTransformer; // Transforme les callouts dans les fichiers .htm
        private readonly FigureTransformer _figureTransformer; // Transforme les figures dans les fichiers .htm
        private readonly SimpleStyleCleanupTransformer _simpleStyleCleanupTransformer; // Nettoie les styles simples dans les fichiers .htm
        private readonly IhmDiagnosticService _ihmDiagnosticService; // Analyse les diagnostics IHM et génère des fichiers JSON pour les variables IHM (à revoir)
        private readonly IhmVariableJsonGenerator _ihmVariableJsonGenerator; // Génère des fichiers JSON pour les variables IHM (à revoir)
        private readonly IhmVariableMatcher _ihmVariableMatcher; // Associe les variables IHM détectées aux fichiers JSON correspondants (à revoir)

        public AitCleanupService()
        {
            _scanner = new HtmlFileScanner(); // Récupérer la liste des fichiers .htm et .html à traiter
            _logService = new CleanupLogService(); // Chargé d'écrire le rapport d'exécution sur disque

            _actionResultDetector = new ActionResultListDetector(); // Détecte les listes d'actions dans les fichiers .htm
            _actionResultTransformer = new ActionResultListTransformer(); // Transforme les listes d'actions détectées en un format standardisé
            _bulletListTransformer = new BulletListTransformer(); // Transforme les listes à puces dans les fichiers .htm
            _calloutTransformer = new CalloutTransformer(); // Transforme les callouts dans les fichiers .htm
            _figureTransformer = new FigureTransformer(); // Transforme les figures dans les fichiers .htm
            _simpleStyleCleanupTransformer = new SimpleStyleCleanupTransformer(); // Nettoie les styles simples dans les fichiers .htm
            _ihmDiagnosticService = new IhmDiagnosticService(); // Analyse les diagnostics IHM et génère des fichiers JSON pour les variables IHM (à revoir)
            _ihmVariableJsonGenerator = new IhmVariableJsonGenerator(); // Génère des fichiers JSON pour les variables IHM (à revoir)
            _ihmVariableMatcher = new IhmVariableMatcher(); // Associe les variables IHM détectées aux fichiers JSON correspondants (à revoir)
        }

        /// <summary>
        ///  Lance le nettoyage AIT selon les options choisies par l'utilisateur.
        ///  Traitement:
        ///  1. Scanne les fichiers .htm à traiter
        ///  2. Applique les transformations cochées dans l'interface
        ///  3. Alimente progressivement le rapport d'exécution
        ///  4. Ecrit le log final, même si une erreur survient pendant le traitement
        ///   </summary>

        public CleanupReport Run(AitCleanupOptions options)
        {
            CleanupReport report = new CleanupReport();

            try
            {
                // Étape 1: Scanner les fichiers .htm à traiter selon projet complet ou dossier sélectionné
                string scanRoot; 
                List<string> files = _scanner.GetHtmlFiles(options, out scanRoot);

                report.ScanRoot = scanRoot;
                report.FilesScanned = files.Count;

                // Étape 2: Transforme d'abord les encadrés et les figures.
                if (options.ProcessCallouts)
                {
                    _calloutTransformer.Transform(files, report);
                }
                if (options.ProcessFigures)
                {
                    _figureTransformer.Transform(files, report);
                }
                // Étape 3: Transforme les listes d'actions et les listes à puces.
                if (options.ProcessActionResults)
                {
                    _actionResultTransformer.Transform(files, report);
                }
                if (options.ProcessBulletLists)
                {
                    _bulletListTransformer.Transform(files, report);
                }
                // Étape 4: Nettoie les styles simples.
                if (options.ProcessStyleCleanup)
                {
                    _simpleStyleCleanupTransformer.Transform(files, report);
                }
                // Étape 5: Analyse les diagnostics IHM et génère des fichiers JSON pour les variables IHM (logique à revoir)
                if (options.ProcessIhm)
                {
                    _ihmDiagnosticService.Analyze(files, report);

                    if (!string.IsNullOrWhiteSpace(options.SourceXmlPath))
                    {
                        string jsonPath = _ihmVariableJsonGenerator.Generate(
                            options.SourceXmlPath,
                            options.TargetPath);

                        report.Warnings.Add("IHM variable JSON generated: " + jsonPath);
                    }
                    else
                    {
                        report.Warnings.Add("IHM variable JSON generation skipped: no Author-it XML source selected.");
                    }

                    _ihmVariableMatcher.Transform(files, report, options.TargetPath);
                }

                report.Warnings.Add("Selected cleanup transformations may have modified HTML files.");
            }
            catch (Exception ex)
            {
                // On stocke l'erreur dans le rapport au lieu de relanccer, pour que l'utilisateur puisse voir un bilan clair ds le log.
                report.Errors.Add(ex.Message); 
            }
            finally
            { 
                // Le log doit être écrit même en cas d'erreur.
                report.FinishedAt = DateTime.Now;
                _logService.WriteLog(options, report);
            }

            return report;
        }
    }
}