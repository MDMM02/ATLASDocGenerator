using System;
using System.IO;
using System.Reflection;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class ResourceCopyService
    {
        public void CopyResources(string projectRootPath, AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                throw new ArgumentException("Project root path is empty.");
            }

            if (!Directory.Exists(projectRootPath))
            {
                throw new DirectoryNotFoundException("Project root folder not found: " + projectRootPath);
            }

            string resourceRoot = GetResourceRoot();

            if (!Directory.Exists(resourceRoot))
            {
                throw new DirectoryNotFoundException("AIT resources folder not found: " + resourceRoot);
            }

            CopyResourceFolder(
                Path.Combine(resourceRoot, "PageLayouts"),
                Path.Combine(projectRootPath, "Content", "Resources", "PageLayouts")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "Stylesheets"),
                Path.Combine(projectRootPath, "Content", "Resources", "Stylesheets")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "Snippets"),
                Path.Combine(projectRootPath, "Content", "Resources", "Snippets")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "Images"),
                Path.Combine(projectRootPath, "Content", "Resources", "Images")
            );

            CopyResourceFolder(
                Path.Combine(resourceRoot, "VariableSets"),
                Path.Combine(projectRootPath, "Project", "VariableSets")
            );
        }

        private string GetResourceRoot()
        {
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrWhiteSpace(pluginFolder))
            {
                throw new DirectoryNotFoundException("Unable to locate plugin folder.");
            }

            return Path.Combine(pluginFolder, "Templates", "AitResources");
        }

        private void CopyResourceFolder(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                return;
            }

            Directory.CreateDirectory(destinationFolder);

            foreach (string sourceFilePath in Directory.GetFiles(sourceFolder))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationFolder, fileName);

                File.Copy(sourceFilePath, destinationFilePath, true);
            }

            foreach (string sourceSubFolder in Directory.GetDirectories(sourceFolder))
            {
                string folderName = Path.GetFileName(sourceSubFolder);
                string destinationSubFolder = Path.Combine(destinationFolder, folderName);

                CopyResourceFolder(sourceSubFolder, destinationSubFolder);
            }
        }
    }
}