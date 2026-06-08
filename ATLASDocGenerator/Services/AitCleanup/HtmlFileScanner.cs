using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class HtmlFileScanner
    {
        public List<string> GetHtmlFiles(AitCleanupOptions options, out string scanRoot)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (string.IsNullOrWhiteSpace(options.TargetPath))
            {
                throw new InvalidOperationException("No target path was provided.");
            }

            if (!Directory.Exists(options.TargetPath))
            {
                throw new DirectoryNotFoundException("Target folder not found: " + options.TargetPath);
            }

            scanRoot = ResolveScanRoot(options);

            if (!Directory.Exists(scanRoot))
            {
                throw new DirectoryNotFoundException("Scan root not found: " + scanRoot);
            }

            List<string> files = Directory
                .EnumerateFiles(scanRoot, "*.htm", SearchOption.AllDirectories)
                .Where(IsValidTopicFile)
                .OrderBy(path => path)
                .ToList();

            return files;
        }

        private string ResolveScanRoot(AitCleanupOptions options)
        {
            if (options.Scope == AitCleanupScope.SelectedFolder)
            {
                return options.TargetPath;
            }

            // Whole project mode:
            // If the user selected the project root, use its Content folder.
            // If the user selected Content directly, use it as-is.
            string folderName = new DirectoryInfo(options.TargetPath).Name;

            if (folderName.Equals("Content", StringComparison.OrdinalIgnoreCase))
            {
                return options.TargetPath;
            }

            string contentFolder = Path.Combine(options.TargetPath, "Content");

            if (Directory.Exists(contentFolder))
            {
                return contentFolder;
            }

            // Fallback: scan selected folder.
            return options.TargetPath;
        }

        private bool IsValidTopicFile(string filePath)
        {
            string normalized = filePath.Replace('/', '\\');

            if (normalized.IndexOf("\\Output\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("\\Analyzer\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("\\Project\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }
    }
}