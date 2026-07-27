using System;
using System.Collections.Generic;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    /// <summary>
    /// 
    /// Classe qui orchestre les différentes opérations de finalisation d'un projet Flare après un import AIT
    /// Selon les options elle peut:
    /// - nettoyer la TOC importée
    /// - copier les ressources nécessaires dans le projet
    /// - mettre à jour les variables générales
    /// - configurer la target Flare
    /// 
    /// </summary>
    public class AitImportFinalizerService
    {
        private readonly AitDocumentProfileFactory _profileFactory; // Retourne le profil orrespondant au type de doc sélectionné
        private readonly TocCleanerService _tocCleanerService; // Supprime les entrées parasites de la TOC importée
        private readonly ResourceCopyService _resourceCopyService; // Copie les ressources nécessaires dans le projet
        private readonly TargetConfiguratorService _targetConfiguratorService; // Configure la target avec la TOc,les styleset layouts attendus
        private readonly VariableSetUpdaterService _variableSetUpdaterService; // Met à jour les variables du fichier General.flar

        // Initialise les différents services utilisés
        public AitImportFinalizerService()
        {
            _profileFactory = new AitDocumentProfileFactory();
            _tocCleanerService = new TocCleanerService();
            _resourceCopyService = new ResourceCopyService();
            _targetConfiguratorService = new TargetConfiguratorService();
            _variableSetUpdaterService = new VariableSetUpdaterService();
        }

        /// <summary>
        /// Lance la finalisation du projet importé selon options choisies par l'utilisateur
        /// 
        /// Traitement:
        /// 1. Vérifie que les options sont présentes
        /// 2. Récupère le profil correspondant au type de document
        /// 3. Nettoie la TOC si option cochée
        /// 4. Copie les ressources si option cochée
        /// 5. Met à jour les variables si option cochée
        /// 6. Configure la target si option cochée
        /// 7. Retourne un rapport avec résultats, warnings et erreurs
        /// 
        /// Opérations entourées de blocs try/catch séparés
        /// Une erreur n'empêche ps l'exécution des autres étapes.
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public AitImportFinalizerReport Run(AitImportFinalizerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            // Récupère la configuration correspondant au profil sélectionné
            AitDocumentProfile profile = _profileFactory.GetProfile(options.DocumentType);

            AitImportFinalizerReport report = new AitImportFinalizerReport
            {
                ProfileName = profile.DisplayName
            };
            // Warning pr faciliter le diagnostic
            report.Warnings.Add("Selected stylesheet: " + profile.PrimaryStylesheet);
            report.Warnings.Add("Selected page layout: " + profile.PrimaryPageLayout);

            // Etape 1: nettoyage de la TOC importée
            if (options.CleanToc)
            {
                try
                {
                    // Supprime les entrées parasites définies ds le profil du document
                    List<string> removedEntries = _tocCleanerService.CleanToc(options.TocPath, profile);

                    report.TocCleaned = true;
                    report.TocEntriesRemoved.AddRange(removedEntries);

                    if (removedEntries.Count == 0)
                    {
                        report.Warnings.Add("TOC cleanup completed, but no matching parasite entry was found.");
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("TOC cleanup failed: " + ex.Message);
                }
            }

            // Etape 2: copie des ressources nécessaires ds le projet
            if (options.CopyResources)
            {
                try
                {
                    _resourceCopyService.CopyResources(options.ProjectRootPath, profile);
                    report.ResourcesCopied = true;
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Resource copy failed: " + ex.Message);
                }
            }
            // Etape 3: mise à jour des variables générales du projet
            if (options.UpdateVariables)
            {
                try
                {
                    _variableSetUpdaterService.UpdateGeneralVariables(options.ProjectRootPath, options);
                    report.VariablesUpdated = true;
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Variable update failed: " + ex.Message);
                }
            }
            // Etape 4: configuration de la target sélectionnée
            if (options.ConfigureTarget)
            {
                try
                {
                    _targetConfiguratorService.ConfigureTarget(options.TargetPath, options.TocPath, profile);
                    report.TargetConfigured = true;
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Target configuration failed: " + ex.Message);
                }
            }

            return report;
        }
    }
}