using System;
using System.Collections.Generic;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class AitCleanupService
    {
        private readonly HtmlFileScanner _scanner;
        private readonly CleanupLogService _logService;
        private readonly ActionResultListDetector _actionResultDetector;
        private readonly ActionResultListTransformer _actionResultTransformer;
        private readonly BulletListTransformer _bulletListTransformer;
        private readonly CalloutTransformer _calloutTransformer;

        public AitCleanupService()
        {
            _scanner = new HtmlFileScanner();
            _logService = new CleanupLogService();
            _actionResultDetector = new ActionResultListDetector();
            _actionResultTransformer = new ActionResultListTransformer();
            _bulletListTransformer = new BulletListTransformer();
            _calloutTransformer = new CalloutTransformer();
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

                if (options.ProcessCallouts)
                {
                    _calloutTransformer.Transform(files, report);
                }

                if (options.ProcessActionResults)
                {
                    _actionResultTransformer.Transform(files, report);
                }
                if (options.ProcessBulletLists)
                {
                    _bulletListTransformer.Transform(files, report);
                }

                report.Warnings.Add("Selected cleanup transformations may have modified HTML files.");
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