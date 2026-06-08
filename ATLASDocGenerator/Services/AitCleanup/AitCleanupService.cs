using System;
using System.Collections.Generic;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class AitCleanupService
    {
        private readonly HtmlFileScanner _scanner;
        private readonly CleanupLogService _logService;

        public AitCleanupService()
        {
            _scanner = new HtmlFileScanner();
            _logService = new CleanupLogService();
        }

        public CleanupReport Run(AitCleanupOptions options)
        {
            CleanupReport report = new CleanupReport();

            try
            {
                string scanRoot;
                List<string> files = _scanner.GetHtmlFiles(options, out scanRoot);

                report.ScanRoot = scanRoot;
                report.FilesScanned = files.Count;

                // Foundation phase only:
                // We scan files, but we do not modify anything yet.
                report.Warnings.Add("Foundation phase only: no XML/HTML transformation has been applied.");
            }
            catch (Exception ex)
            {
                report.Errors.Add(ex.Message);
            }
            finally
            {
                report.FinishedAt = DateTime.Now;
                _logService.WriteLog(options, report);
            }

            return report;
        }
    }
}