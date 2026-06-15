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

            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrWhiteSpace(pluginFolder))
            {
                throw new DirectoryNotFoundException("Unable to locate plugin folder.");
            }

            string sourceRoot = Path.Combine(pluginFolder, "Templates", "AitResources");

            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException("AIT resources template folder not found: " + sourceRoot);
            }

            CopyDirectory(sourceRoot, projectRootPath);
        }

        private void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (string sourceFilePath in Directory.GetFiles(sourceDirectory))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationDirectory, fileName);

                File.Copy(sourceFilePath, destinationFilePath, true);
            }

            foreach (string sourceSubDirectory in Directory.GetDirectories(sourceDirectory))
            {
                string directoryName = Path.GetFileName(sourceSubDirectory);
                string destinationSubDirectory = Path.Combine(destinationDirectory, directoryName);

                CopyDirectory(sourceSubDirectory, destinationSubDirectory);
            }
        }
    }
}