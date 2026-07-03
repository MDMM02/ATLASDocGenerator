using ATLASDocGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class IhmDiagnosticService
    {
        private static readonly string[] IhmKeywords =
        {
            "ihm",
            "menu",
            "trad"
        };

        public void Analyze(List<string> files, CleanupReport report)
        {
            if (files == null || report == null)
                return;

            foreach (string filePath in files)
            {
                try
                {
                    XDocument document = XDocument.Load(filePath);
                    AnalyzeDocument(document, filePath, report);
                }
                catch (Exception ex)
                {
                    report.Warnings.Add("IHM diagnostic skipped file: " + filePath + " - " + ex.Message);
                }
            }
        }

        private void AnalyzeDocument(XDocument document, string filePath, CleanupReport report)
        {
            if (document == null)
                return;

            IEnumerable<XElement> elementsWithClass = document
                .Descendants()
                .Where(e => e.Attribute("class") != null);

            foreach (XElement element in elementsWithClass)
            {
                string classAttribute = (string)element.Attribute("class");

                foreach (string className in SplitClasses(classAttribute))
                {
                    report.AddIhmClassOccurrence(className, filePath);
                }
            }
        }

        private IEnumerable<string> SplitClasses(string classAttribute)
        {
            if (string.IsNullOrWhiteSpace(classAttribute))
                return Enumerable.Empty<string>();

            return classAttribute
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c));
        }

        private bool IsPotentialIhmClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return false;

            return IhmKeywords.Any(keyword =>
                className.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}