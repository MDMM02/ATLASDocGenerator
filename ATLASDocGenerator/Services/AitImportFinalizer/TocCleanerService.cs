using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class TocCleanerService
    {
        public List<string> CleanToc(string tocPath, AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(tocPath))
            {
                throw new ArgumentException("TOC path is empty.");
            }

            if (!File.Exists(tocPath))
            {
                throw new FileNotFoundException("TOC file not found.", tocPath);
            }

            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            XDocument document = XDocument.Load(tocPath, LoadOptions.PreserveWhitespace);

            List<XElement> entriesToRemove = document
                .Descendants()
                .Where(element => element.Name.LocalName == "TocEntry")
                .Where(element => ShouldRemoveEntry(element, profile.TocEntriesToRemove))
                .ToList();

            List<string> removedEntries = new List<string>();

            foreach (XElement entry in entriesToRemove)
            {
                string label = GetEntryLabel(entry);
                removedEntries.Add(label);

                entry.Remove();
            }

            if (removedEntries.Count > 0)
            {
                document.Save(tocPath);
            }

            return removedEntries;
        }

        private bool ShouldRemoveEntry(XElement entry, List<string> patterns)
        {
            string title = GetAttributeValue(entry, "Title");
            string link = GetAttributeValue(entry, "Link");
            string source = GetAttributeValue(entry, "Source");

            string combined = (title + " " + link + " " + source).ToLowerInvariant();

            foreach (string pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                if (combined.Contains(pattern.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetEntryLabel(XElement entry)
        {
            string title = GetAttributeValue(entry, "Title");

            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            string link = GetAttributeValue(entry, "Link");

            if (!string.IsNullOrWhiteSpace(link))
            {
                return link;
            }

            return "(unnamed TOC entry)";
        }

        private string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return string.Empty;
            }

            return attribute.Value ?? string.Empty;
        }
    }
}