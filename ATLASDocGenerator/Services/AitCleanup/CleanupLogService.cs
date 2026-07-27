using System;
using System.IO;
using System.Text;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class CleanupLogService
    {
        public string WriteLog(
            AitCleanupOptions options,
            CleanupReport report)
        {
            if (options == null)
            {
                throw new ArgumentNullException(
                    "options");
            }

            if (report == null)
            {
                throw new ArgumentNullException(
                    "report");
            }

            string logFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "ATLASDocGenerator",
                "AITCleanupLogs");

            Directory.CreateDirectory(logFolder);

            string fileName =
                "AIT_Cleanup_Log_"
                + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                + ".txt";

            string logPath =
                Path.Combine(
                    logFolder,
                    fileName);

            StringBuilder log =
                new StringBuilder();

            WriteHeader(log, report);

            WriteOptions(
                log,
                options,
                report);

            WriteSelectedTransformations(
                log,
                options);

            WriteGlobalResults(
                log,
                report);

            WriteActionResultDetails(
                log,
                report);

            WriteBulletDetails(
                log,
                report);

            WriteFigureDetails(
                log,
                report);

            WriteCalloutDetails(
                log,
                report);

            WriteStyleCleanupDetails(
                log,
                report);

            if (options.ProcessIhm)
            {
                WriteIhmVariableResults(
                    log,
                    report);
            }

            WriteWarnings(
                log,
                report);

            WriteErrors(
                log,
                report);

            log.AppendLine();
            log.AppendLine("NOTE");
            log.AppendLine("------------------------------");
            log.AppendLine(
                "Selected transformations may have modified project files. "
                + "Review the counters, details and backups before running the cleanup again.");

            File.WriteAllText(
                logPath,
                log.ToString(),
                Encoding.UTF8);

            report.LogFilePath =
                logPath;

            return logPath;
        }

        private void WriteHeader(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine("AIT CLEANUP LOG");
            log.AppendLine("==============================");

            log.AppendLine(
                "Started at: "
                + report.StartedAt);

            log.AppendLine(
                "Finished at: "
                + report.FinishedAt);

            log.AppendLine();
        }

        private void WriteOptions(
            StringBuilder log,
            AitCleanupOptions options,
            CleanupReport report)
        {
            log.AppendLine("OPTIONS");
            log.AppendLine("------------------------------");

            log.AppendLine(
                "Documentation type: "
                + options.DocumentationType);

            log.AppendLine(
                "Scope: "
                + options.Scope);

            log.AppendLine(
                "Target path: "
                + options.TargetPath);

            log.AppendLine(
                "Scan root: "
                + report.ScanRoot);

            if (options.ProcessIhm)
            {
                log.AppendLine(
                    "Author-it XML source: "
                    + options.SourceXmlPath);

                log.AppendLine(
                    "Selected IHM templates: "
                    + GetSelectedTemplateIds(options));
            }

            log.AppendLine();
        }

        private void WriteSelectedTransformations(
            StringBuilder log,
            AitCleanupOptions options)
        {
            log.AppendLine("SELECTED TRANSFORMATIONS");
            log.AppendLine("------------------------------");

            log.AppendLine(
                "Action/result lists: "
                + YesNo(options.ProcessActionResults));

            log.AppendLine(
                "Bullet lists: "
                + YesNo(options.ProcessBulletLists));

            log.AppendLine(
                "Callouts: "
                + YesNo(options.ProcessCallouts));

            log.AppendLine(
                "Figures: "
                + YesNo(options.ProcessFigures));

            log.AppendLine(
                "Style cleanup: "
                + YesNo(options.ProcessStyleCleanup));

            log.AppendLine(
                "IHM / variables: "
                + YesNo(options.ProcessIhm));

            log.AppendLine();
        }

        private void WriteGlobalResults(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine("RESULTS");
            log.AppendLine("------------------------------");

            log.AppendLine(
                "Files scanned: "
                + report.FilesScanned);

            log.AppendLine(
                "Action num paragraphs detected: "
                + report.ActionNumParagraphsDetected);

            log.AppendLine(
                "Action bullet paragraphs detected: "
                + report.ActionBulletParagraphsDetected);

            log.AppendLine(
                "Result paragraphs detected: "
                + report.ResultParagraphsDetected);

            log.AppendLine(
                "Action/result lists transformed: "
                + report.ActionResultListsTransformed);

            log.AppendLine(
                "Bullet lists transformed: "
                + report.BulletListsTransformed);

            log.AppendLine(
                "Bullet paragraphs detected: "
                + report.BulletParagraphsDetected);

            log.AppendLine(
                "a_NOpagebreak blocks created: "
                + report.NoPageBreakBlocksCreated);

            log.AppendLine(
                "Callouts transformed: "
                + report.CalloutsTransformed);

            log.AppendLine(
                "Figures transformed: "
                + report.FiguresTransformed);

            log.AppendLine(
                "Styles cleaned: "
                + report.StylesCleaned);

            log.AppendLine();
        }

        private void WriteActionResultDetails(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine(
                "ACTION / RESULT TRANSFORMATION DETAILS");

            log.AppendLine(
                "------------------------------");

            if (report.ActionResultDetectionDetails == null
                || report.ActionResultDetectionDetails.Count == 0)
            {
                log.AppendLine(
                    "No action/result paragraph detected.");
            }
            else
            {
                foreach (string detail
                    in report.ActionResultDetectionDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();
        }

        private void WriteBulletDetails(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine(
                "BULLET LIST TRANSFORMATION DETAILS");

            log.AppendLine(
                "------------------------------");

            if (report.BulletListTransformationDetails == null
                || report.BulletListTransformationDetails.Count == 0)
            {
                log.AppendLine(
                    "No bullet list paragraph transformed.");
            }
            else
            {
                foreach (string detail
                    in report.BulletListTransformationDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();
        }

        private void WriteFigureDetails(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine(
                "FIGURE TRANSFORMATION DETAILS");

            log.AppendLine(
                "------------------------------");

            if (report.FigureTransformationDetails == null
                || report.FigureTransformationDetails.Count == 0)
            {
                log.AppendLine(
                    "No figure transformed.");
            }
            else
            {
                foreach (string detail
                    in report.FigureTransformationDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();
        }

        private void WriteCalloutDetails(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine(
                "CALLOUT TRANSFORMATION DETAILS");

            log.AppendLine(
                "------------------------------");

            if (report.CalloutTransformationDetails == null
                || report.CalloutTransformationDetails.Count == 0)
            {
                log.AppendLine(
                    "No callout transformed.");
            }
            else
            {
                foreach (string detail
                    in report.CalloutTransformationDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();
        }

        private void WriteStyleCleanupDetails(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine(
                "STYLE CLEANUP DETAILS");

            log.AppendLine(
                "------------------------------");

            if (report.StyleCleanupDetails == null
                || report.StyleCleanupDetails.Count == 0)
            {
                log.AppendLine(
                    "No simple style cleaned.");
            }
            else
            {
                foreach (string detail
                    in report.StyleCleanupDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();
        }

        private void WriteIhmVariableResults(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine(
                "IHM VARIABLE GENERATION RESULTS");

            log.AppendLine(
                "------------------------------");

            log.AppendLine(
                "Variable sets generated: "
                + report.IhmVariableSetsGenerated);

            log.AppendLine(
                "Variables generated: "
                + report.IhmVariablesGenerated);

            log.AppendLine(
                "Content files scanned for IHM references: "
                + report.IhmReferenceFilesScanned);

            log.AppendLine(
                "Content files modified: "
                + report.IhmReferenceFilesModified);

            log.AppendLine(
                "Snippet references replaced: "
                + report.IhmReferencesReplaced);

            log.AppendLine(
                "Unmatched Topic IDs: "
                + report.IhmUnmatchedTopicIds);

            log.AppendLine();

            log.AppendLine(
                "GENERATED VARIABLE SETS");

            log.AppendLine(
                "------------------------------");

            if (report.IhmVariableSetGenerationDetails == null
                || report.IhmVariableSetGenerationDetails.Count == 0)
            {
                log.AppendLine(
                    "No variable set generated.");
            }
            else
            {
                foreach (string detail
                    in report.IhmVariableSetGenerationDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();

            log.AppendLine(
                "IHM REFERENCE REPLACEMENT DETAILS");

            log.AppendLine(
                "------------------------------");

            if (report.IhmReferenceReplacementDetails == null
                || report.IhmReferenceReplacementDetails.Count == 0)
            {
                log.AppendLine(
                    "No snippet reference replaced.");
            }
            else
            {
                foreach (string detail
                    in report.IhmReferenceReplacementDetails)
                {
                    log.AppendLine(
                        "- "
                        + detail);
                }
            }

            log.AppendLine();
        }

        private void WriteWarnings(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine("WARNINGS");
            log.AppendLine("------------------------------");

            if (report.Warnings == null
                || report.Warnings.Count == 0)
            {
                log.AppendLine("None");
            }
            else
            {
                foreach (string warning
                    in report.Warnings)
                {
                    log.AppendLine(
                        "- "
                        + warning);
                }
            }

            log.AppendLine();
        }

        private void WriteErrors(
            StringBuilder log,
            CleanupReport report)
        {
            log.AppendLine("ERRORS");
            log.AppendLine("------------------------------");

            if (report.Errors == null
                || report.Errors.Count == 0)
            {
                log.AppendLine("None");
            }
            else
            {
                foreach (string error
                    in report.Errors)
                {
                    log.AppendLine(
                        "- "
                        + error);
                }
            }
        }

        private string GetSelectedTemplateIds(
            AitCleanupOptions options)
        {
            if (options.SelectedIhmTemplateIds == null
                || options.SelectedIhmTemplateIds.Count == 0)
            {
                return "None";
            }

            return string.Join(
                ", ",
                options.SelectedIhmTemplateIds);
        }

        private string YesNo(bool value)
        {
            return value
                ? "Yes"
                : "No";
        }
    }
}