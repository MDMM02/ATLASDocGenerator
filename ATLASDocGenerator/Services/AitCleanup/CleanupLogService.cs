using System;
using System.IO;
using System.Text;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class CleanupLogService
    {
        public string WriteLog(AitCleanupOptions options, CleanupReport report)
        {
            string logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ATLASDocGenerator",
                "AITCleanupLogs"
            );

            Directory.CreateDirectory(logFolder);

            string fileName = "AIT_Cleanup_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            string logPath = Path.Combine(logFolder, fileName);

            StringBuilder log = new StringBuilder();

            log.AppendLine("AIT CLEANUP LOG");
            log.AppendLine("==============================");
            log.AppendLine("Started at: " + report.StartedAt);
            log.AppendLine("Finished at: " + report.FinishedAt);
            log.AppendLine();

            log.AppendLine("OPTIONS");
            log.AppendLine("------------------------------");
            log.AppendLine("Documentation type: " + options.DocumentationType);
            log.AppendLine("Scope: " + options.Scope);
            log.AppendLine("Target path: " + options.TargetPath);
            log.AppendLine("Scan root: " + report.ScanRoot);
            log.AppendLine();

            log.AppendLine("SELECTED TRANSFORMATIONS");
            log.AppendLine("------------------------------");
            log.AppendLine("Action/result lists: " + YesNo(options.ProcessActionResults));
            log.AppendLine("Bullet lists: " + YesNo(options.ProcessBulletLists));
            log.AppendLine("Callouts: " + YesNo(options.ProcessCallouts));
            log.AppendLine("Figures: " + YesNo(options.ProcessFigures));
            log.AppendLine("Style cleanup: " + YesNo(options.ProcessStyleCleanup));
            log.AppendLine("IHM / variables: " + YesNo(options.ProcessIhm));
            log.AppendLine();

            log.AppendLine("RESULTS");
            log.AppendLine("------------------------------");
            log.AppendLine("Files scanned: " + report.FilesScanned);
            log.AppendLine("Action num paragraphs detected: " + report.ActionNumParagraphsDetected);
            log.AppendLine("Action bullet paragraphs detected: " + report.ActionBulletParagraphsDetected);
            log.AppendLine("Result paragraphs detected: " + report.ResultParagraphsDetected); 
            log.AppendLine("Action/result lists transformed: " + report.ActionResultListsTransformed);
            log.AppendLine("Bullet lists transformed: " + report.BulletListsTransformed);
            log.AppendLine("Callouts transformed: " + report.CalloutsTransformed);
            log.AppendLine("Figures transformed: " + report.FiguresTransformed);
            log.AppendLine("Styles cleaned: " + report.StylesCleaned);
            log.AppendLine("IHM items detected: " + report.IhmItemsDetected);
            log.AppendLine();
            log.AppendLine();

            log.AppendLine("ACTION / RESULT DETECTION DETAILS");
            log.AppendLine("------------------------------");

            if (report.ActionResultDetectionDetails.Count == 0)
            {
                log.AppendLine("No action/result paragraph detected.");
            }
            else
            {
                foreach (string detail in report.ActionResultDetectionDetails)
                {
                    log.AppendLine("- " + detail);
                }
            }
            log.AppendLine();
            log.AppendLine("WARNINGS");
            log.AppendLine("------------------------------");

            if (report.Warnings.Count == 0)
            {
                log.AppendLine("None");
            }
            else
            {
                foreach (string warning in report.Warnings)
                {
                    log.AppendLine("- " + warning);
                }
            }

            log.AppendLine();

            log.AppendLine("ERRORS");
            log.AppendLine("------------------------------");

            if (report.Errors.Count == 0)
            {
                log.AppendLine("None");
            }
            else
            {
                foreach (string error in report.Errors)
                {
                    log.AppendLine("- " + error);
                }
            }

            log.AppendLine();
            log.AppendLine("NOTE");
            log.AppendLine("------------------------------");
            log.AppendLine("Selected transformations may have been applied. Check the counters and details above.");
            File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);

            report.LogFilePath = logPath;

            return logPath;
        }

        private string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}