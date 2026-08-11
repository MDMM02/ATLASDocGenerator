using System;
using System.IO;
using System.Text;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitCleanup;
using ATLASDocGenerator.Services.AitImportFinalizer;

namespace ATLASDocGenerator.Services
{
    public class AitWorkflowService
    {
        public AitWorkflowReport Run(AitWorkflowOptions options)
        {
            ValidateOptions(options);
            AitDocumentProfile profile =
                new AitDocumentProfileFactory().GetProfile(options.DocumentType);
            AitWorkflowReport report = new AitWorkflowReport
            {
                ProfileName = profile.DisplayName
            };

            if (options.InstallResources)
            {
                try
                {
                    report.ResourceCopyResult =
                        new ResourceCopyService().CopyResources(options.ProjectRootPath, profile);
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Installation des ressources : " + ex.Message);
                }
            }

            if (options.CleanContent || options.ProcessIhm)
            {
                try
                {
                    AitCleanupOptions cleanup = options.CleanupOptions ?? new AitCleanupOptions();
                    cleanup.ProcessIhm = options.ProcessIhm;
                    if (!options.CleanContent)
                    {
                        cleanup.ProcessActionResults = false;
                        cleanup.ProcessBulletLists = false;
                        cleanup.ProcessCallouts = false;
                        cleanup.ProcessFigures = false;
                        cleanup.ProcessStyleCleanup = false;
                    }
                    report.CleanupReport = new AitCleanupService().Run(cleanup);
                    report.Errors.AddRange(report.CleanupReport.Errors);
                    report.Warnings.AddRange(report.CleanupReport.Warnings);
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Nettoyage des contenus : " + ex.Message);
                }
            }

            if (options.CleanToc)
            {
                try
                {
                    report.TocEntriesRemoved.AddRange(
                        new TocCleanerService().CleanToc(options.TocPath, profile));
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Nettoyage de la TOC : " + ex.Message);
                }
            }

            if (options.VerifyTarget || options.RepairTarget)
            {
                TargetConfiguratorService targetService = new TargetConfiguratorService();
                try
                {
                    TargetValidationResult beforeRepair = targetService.ValidateTarget(
                        options.TargetPath,
                        options.TocPath,
                        profile);
                    report.TargetValidation = beforeRepair;

                    if (options.RepairTarget && !beforeRepair.IsValid)
                    {
                        targetService.ConfigureTarget(options.TargetPath, options.TocPath, profile);
                        report.TargetRepaired = true;
                        report.TargetValidation = targetService.ValidateTarget(
                            options.TargetPath,
                            options.TocPath,
                            profile);
                    }

                    if (!report.TargetValidation.IsValid)
                    {
                        foreach (TargetConfigurationDifference difference
                            in report.TargetValidation.Differences)
                        {
                            report.Warnings.Add(
                                "Target " + difference.SettingName
                                + " : '" + difference.CurrentValue
                                + "' au lieu de '" + difference.ExpectedValue + "'.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Vérification de la target : " + ex.Message);
                }
            }

            if (options.GenerateReport)
            {
                try
                {
                    report.ReportFilePath = WriteReport(options, report);
                }
                catch (Exception ex)
                {
                    report.Warnings.Add("Rapport final non enregistré : " + ex.Message);
                }
            }

            return report;
        }

        private void ValidateOptions(AitWorkflowOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (string.IsNullOrWhiteSpace(options.ProjectRootPath)
                || !Directory.Exists(options.ProjectRootPath))
                throw new DirectoryNotFoundException("La racine du projet Flare est introuvable.");
            if ((options.CleanToc || options.VerifyTarget || options.RepairTarget)
                && (string.IsNullOrWhiteSpace(options.TocPath) || !File.Exists(options.TocPath)))
                throw new FileNotFoundException("La TOC sélectionnée est introuvable.", options.TocPath);
            if ((options.VerifyTarget || options.RepairTarget)
                && (string.IsNullOrWhiteSpace(options.TargetPath) || !File.Exists(options.TargetPath)))
                throw new FileNotFoundException("La target sélectionnée est introuvable.", options.TargetPath);
            if (!options.InstallResources
                && !options.CleanContent
                && !options.ProcessIhm
                && !options.CleanToc
                && !options.VerifyTarget
                && !options.RepairTarget)
                throw new InvalidOperationException("Sélectionnez au moins une action.");
        }

        private string WriteReport(AitWorkflowOptions options, AitWorkflowReport report)
        {
            string folder = Path.Combine(options.ProjectRootPath, "Project", "AITWorkflowLogs");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(
                folder,
                "AIT_Finalization_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
            StringBuilder text = new StringBuilder();
            text.AppendLine("Finalisation import AIT");
            text.AppendLine("Profil : " + report.ProfileName);
            text.AppendLine("Projet : " + options.ProjectRootPath);
            text.AppendLine("General.flvar : préservé s'il existait");
            text.AppendLine("Ressources ATLAS : " + (options.InstallResources ? "demandées" : "ignorées"));
            text.AppendLine("Cleanup topics/snippets : " + (options.CleanContent ? "demandé" : "ignoré"));
            text.AppendLine("Variables IHM : " + (options.ProcessIhm ? "demandées" : "ignorées"));
            text.AppendLine("Nettoyage TOC : " + (options.CleanToc ? "demandé" : "ignoré"));
            text.AppendLine("Vérification target : " + (options.VerifyTarget ? "demandée" : "ignorée"));
            text.AppendLine("Réparation target : " + (options.RepairTarget ? "autorisée" : "interdite"));

            if (report.ResourceCopyResult != null)
            {
                text.AppendLine("Ressources ajoutées : " + report.ResourceCopyResult.FilesCopied);
                text.AppendLine("Ressources mises à jour : " + report.ResourceCopyResult.FilesUpdated);
                text.AppendLine("Ressources préservées : " + report.ResourceCopyResult.FilesPreserved);
            }
            if (report.CleanupReport != null)
            {
                text.AppendLine("Fichiers scannés : " + report.CleanupReport.FilesScanned);
                text.AppendLine("Listes action/résultat : " + report.CleanupReport.ActionResultListsTransformed);
                text.AppendLine("Listes à tirets : " + report.CleanupReport.BulletListsTransformed);
                text.AppendLine("Callouts : " + report.CleanupReport.CalloutsTransformed);
                text.AppendLine("Figures : " + report.CleanupReport.FiguresTransformed);
                text.AppendLine("Styles nettoyés : " + report.CleanupReport.StylesCleaned);
                text.AppendLine("Jeux de variables IHM générés : " + report.CleanupReport.IhmVariableSetsGenerated);
                text.AppendLine("Références IHM remplacées : " + report.CleanupReport.IhmReferencesReplaced);
                text.AppendLine("Log cleanup : " + report.CleanupReport.LogFilePath);
            }
            text.AppendLine("Entrées TOC supprimées : " + report.TocEntriesRemoved.Count);
            foreach (string entry in report.TocEntriesRemoved)
                text.AppendLine("  - " + entry);
            if (report.TargetValidation != null)
            {
                text.AppendLine("Target conforme : " + (report.TargetValidation.IsValid ? "oui" : "non"));
                foreach (TargetConfigurationDifference difference in report.TargetValidation.Differences)
                {
                    text.AppendLine(
                        "  - " + difference.SettingName + " : '"
                        + difference.CurrentValue + "' au lieu de '"
                        + difference.ExpectedValue + "'");
                }
            }
            text.AppendLine("Target réparée : " + (report.TargetRepaired ? "oui" : "non"));

            foreach (string warning in report.Warnings)
                text.AppendLine("AVERTISSEMENT : " + warning);
            foreach (string error in report.Errors)
                text.AppendLine("ERREUR : " + error);

            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            return path;
        }
    }
}
