using System;
using System.IO;
using System.Text;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitImportFinalizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class ResourceCopyServiceTests
    {
        private string _temporaryDirectory;

        [TestInitialize]
        public void CreateTemporaryDirectory()
        {
            _temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "ATLASDocGenerator.Tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_temporaryDirectory);
        }

        [TestCleanup]
        public void DeleteTemporaryDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_temporaryDirectory)
                && Directory.Exists(_temporaryDirectory))
            {
                Directory.Delete(_temporaryDirectory, true);
            }
        }

        [TestMethod]
        public void CopyResources_BacksUpChangedFilesAndSkipsUnchangedFiles()
        {
            string packageRoot = CreateCompleteResourcePackage();
            string projectRoot = CreateFlareProject();

            string existingStylesheet = Path.Combine(
                projectRoot,
                "Content",
                "Resources",
                "Stylesheets",
                "Styles.css");

            WriteFile(existingStylesheet, "ancienne feuille");

            ResourceCopyService service =
                new ResourceCopyService(packageRoot);

            ResourceCopyResult firstResult = service.CopyResources(
                projectRoot,
                CreateProfile());

            Assert.AreEqual(5, firstResult.FilesCopied);
            Assert.AreEqual(1, firstResult.FilesUpdated);
            Assert.AreEqual(0, firstResult.FilesUnchanged);
            Assert.AreEqual(1, firstResult.BackupsCreated);
            Assert.AreEqual("nouvelle feuille", File.ReadAllText(existingStylesheet));
            Assert.AreEqual(
                "ancienne feuille",
                File.ReadAllText(
                    existingStylesheet + ".before-ait-finalizer.bak"));

            ResourceCopyResult secondResult = service.CopyResources(
                projectRoot,
                CreateProfile());

            Assert.AreEqual(0, secondResult.FilesCopied);
            Assert.AreEqual(0, secondResult.FilesUpdated);
            Assert.AreEqual(6, secondResult.FilesUnchanged);
            Assert.AreEqual(0, secondResult.BackupsCreated);
            Assert.AreEqual(
                "ancienne feuille",
                File.ReadAllText(
                    existingStylesheet + ".before-ait-finalizer.bak"));
        }

        [TestMethod]
        public void CopyResources_ValidatesWholePackageBeforeFirstWrite()
        {
            string packageRoot = CreateCompleteResourcePackage();
            string projectRoot = CreateFlareProject();

            Directory.Delete(
                Path.Combine(packageRoot, "Images"),
                true);

            ResourceCopyService service =
                new ResourceCopyService(packageRoot);

            Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
                service.CopyResources(projectRoot, CreateProfile()));

            Assert.IsFalse(Directory.Exists(
                Path.Combine(projectRoot, "Content", "Resources")));
        }

        [TestMethod]
        public void CopyResources_PreservesCustomizedGeneralVariablesOnRerun()
        {
            string packageRoot = CreateCompleteResourcePackage();
            string projectRoot = CreateFlareProject();
            string generalPath = Path.Combine(
                projectRoot,
                "Project",
                "VariableSets",
                "General.flvar");

            WriteFile(generalPath, "variables importées");

            ResourceCopyService service =
                new ResourceCopyService(packageRoot);
            ResourceCopyResult firstResult = service.CopyResources(
                projectRoot,
                CreateProfile());

            Assert.AreEqual("variables", File.ReadAllText(generalPath));
            Assert.AreEqual(1, firstResult.FilesUpdated);

            WriteFile(generalPath, "variables personnalisées");
            ResourceCopyResult secondResult = service.CopyResources(
                projectRoot,
                CreateProfile());

            Assert.AreEqual(
                "variables personnalisées",
                File.ReadAllText(generalPath));
            Assert.AreEqual(1, secondResult.FilesPreserved);
            Assert.AreEqual(0, secondResult.FilesUpdated);
        }

        private string CreateCompleteResourcePackage()
        {
            string packageRoot = Path.Combine(_temporaryDirectory, "Templates");

            WriteFile(
                Path.Combine(packageRoot, "PageLayouts", "Tech.flpgl"),
                "layout");
            WriteFile(
                Path.Combine(packageRoot, "Stylesheets", "Styles.css"),
                "nouvelle feuille");
            WriteFile(
                Path.Combine(packageRoot, "Snippets", "Sample.flsnp"),
                "snippet");
            WriteFile(
                Path.Combine(packageRoot, "Images", "Sample.png"),
                "image");
            WriteFile(
                Path.Combine(packageRoot, "VariableSets", "General.flvar"),
                "variables");
            WriteFile(
                Path.Combine(packageRoot, "Commun Stago", "Sample.htm"),
                "commun");

            return packageRoot;
        }

        private string CreateFlareProject()
        {
            string projectRoot = Path.Combine(_temporaryDirectory, "ProjectRoot");
            Directory.CreateDirectory(Path.Combine(projectRoot, "Content"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Project"));
            return projectRoot;
        }

        private AitDocumentProfile CreateProfile()
        {
            return new AitDocumentProfile
            {
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech.flpgl"
            };
        }

        private void WriteFile(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
}
