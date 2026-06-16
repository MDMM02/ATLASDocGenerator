using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class TargetConfiguratorService
    {
        public void ConfigureTarget(string targetPath, string tocPath, AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new ArgumentException("Target path is empty.");
            }

            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException("Target file not found.", targetPath);
            }

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

            CreateBackup(targetPath);

            XDocument document = XDocument.Load(targetPath, LoadOptions.PreserveWhitespace);

            if (document.Root == null)
            {
                throw new InvalidOperationException("Target file has no XML root.");
            }

            string tocValue = ConvertTocPathToFlarePath(tocPath);

            // Important: for target UI fields, keep paths relative to Content.
            // Example: Resources/Stylesheets/Styles_BT_Test.css
            string stylesheetValue = NormalizeContentRelativePath(profile.PrimaryStylesheet);
            string pageLayoutValue = NormalizeContentRelativePath(profile.PrimaryPageLayout);

            SetAttributeValue(
                document,
                new[] { "MasterToc", "MasterTOC", "PrimaryToc", "PrimaryTOC", "Toc", "TOC" },
                "MasterToc",
                tocValue
            );

            SetAttributeValue(
                document,
                new[] { "MasterStylesheet", "PrimaryStylesheet", "Stylesheet" },
                "MasterStylesheet",
                stylesheetValue
            );

            SetAttributeValue(
                document,
                new[] { "MasterPageLayout", "PrimaryPageLayout", "PageLayout" },
                "MasterPageLayout",
                pageLayoutValue
            );

            document.Save(targetPath);
        }

        private void CreateBackup(string filePath)
        {
            string backupPath = filePath + ".bak";

            if (!File.Exists(backupPath))
            {
                File.Copy(filePath, backupPath);
            }
        }

        private string ConvertTocPathToFlarePath(string tocPath)
        {
            string normalizedPath = tocPath.Replace("\\", "/");

            int projectIndex = normalizedPath.IndexOf("/Project/", StringComparison.OrdinalIgnoreCase);

            if (projectIndex >= 0)
            {
                return normalizedPath.Substring(projectIndex + 1);
            }

            return "Project/TOCs/" + Path.GetFileName(tocPath);
        }

        private string NormalizeContentRelativePath(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return string.Empty;
            }

            string normalizedPath = resourcePath.Replace("\\", "/").TrimStart('/');

            if (normalizedPath.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring("Content/".Length);
            }

            return normalizedPath;
        }

        private void SetAttributeValue(
            XDocument document,
            string[] possibleAttributeNames,
            string defaultAttributeName,
            string value)
        {
            XElement root = document.Root;

            if (root == null)
            {
                throw new InvalidOperationException("Target file has no XML root.");
            }

            XAttribute existingAttribute = root
                .DescendantsAndSelf()
                .SelectMany(element => element.Attributes())
                .FirstOrDefault(attribute =>
                    possibleAttributeNames.Any(name =>
                        attribute.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase)));

            if (existingAttribute != null)
            {
                existingAttribute.Value = MatchExistingPathStyle(existingAttribute.Value, value);
                return;
            }

            root.SetAttributeValue(defaultAttributeName, value);
        }

        private string MatchExistingPathStyle(string existingValue, string newValue)
        {
            if (!string.IsNullOrWhiteSpace(existingValue)
                && existingValue.StartsWith("/")
                && !newValue.StartsWith("/"))
            {
                return "/" + newValue;
            }

            return newValue;
        }
    }
}