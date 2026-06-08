using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class ActionResultListDetector
    {
        public void Detect(IEnumerable<string> htmlFiles, CleanupReport report)
        {
            if (htmlFiles == null)
            {
                throw new ArgumentNullException("htmlFiles");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            foreach (string filePath in htmlFiles)
            {
                try
                {
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int actionNumCount = 0;
                    int actionBulletCount = 0;
                    int resultCount = 0;

                    IEnumerable<XElement> paragraphs = document
                        .Descendants()
                        .Where(element => element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase));

                    foreach (XElement paragraph in paragraphs)
                    {
                        if (HasClass(paragraph, "a_action_num"))
                        {
                            actionNumCount++;
                        }

                        if (HasClass(paragraph, "a_action") || HasClass(paragraph, "a_action_b"))
                        {
                            actionBulletCount++;
                        }

                        if (HasClass(paragraph, "a_resultat") || HasClass(paragraph, "a_resultat_b"))
                        {
                            resultCount++;
                        }
                    }

                    if (actionNumCount > 0 || actionBulletCount > 0 || resultCount > 0)
                    {
                        report.ActionNumParagraphsDetected += actionNumCount;
                        report.ActionBulletParagraphsDetected += actionBulletCount;
                        report.ResultParagraphsDetected += resultCount;

                        string detail =
                            Path.GetFileName(filePath)
                            + " | a_action_num: " + actionNumCount
                            + " | a_action/a_action_b: " + actionBulletCount
                            + " | a_resultat/a_resultat_b: " + resultCount;

                        report.ActionResultDetectionDetails.Add(detail);
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Action/result detection failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        private bool HasClass(XElement element, string className)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
            {
                return false;
            }

            string[] classes = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return classes.Any(c => c.Equals(className, StringComparison.OrdinalIgnoreCase));
        }
    }
}