using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitImportFinalizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class TargetConfiguratorServiceTests
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
        public void ConfigureTarget_UpdatesUniqueSettingsAndCreatesBackup()
        {
            string targetPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "Targets", "Doc.fltar"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<CatapultTarget><PrintedOutput MasterToc=\"old-toc\" "
                + "MasterStylesheet=\"old-style\" MasterPageLayout=\"old-layout\" />"
                + "</CatapultTarget>");

            string tocPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "TOCs", "Doc.fltoc"),
                "<CatapultToc />");

            string original = File.ReadAllText(targetPath);

            TargetConfiguratorService service =
                new TargetConfiguratorService();

            service.ConfigureTarget(targetPath, tocPath, CreateProfile());

            Assert.AreEqual(
                original,
                File.ReadAllText(targetPath + ".bak"));

            XDocument document = XDocument.Load(targetPath);
            XElement printedOutput = document.Root.Element("PrintedOutput");

            Assert.AreEqual(
                "Project/TOCs/Doc.fltoc",
                printedOutput.Attribute("MasterToc").Value);
            Assert.AreEqual(
                "/Content/Resources/Stylesheets/Styles.css",
                printedOutput.Attribute("MasterStylesheet").Value);
            Assert.AreEqual(
                "/Content/Resources/PageLayouts/Tech.flpgl",
                printedOutput.Attribute("MasterPageLayout").Value);
        }

        [TestMethod]
        public void ConfigureTarget_RejectsAmbiguousDescendantSettingsWithoutWriting()
        {
            string original =
                "<CatapultTarget><First MasterStylesheet=\"one.css\" />"
                + "<Second PrimaryStylesheet=\"two.css\" /></CatapultTarget>";

            string targetPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "Targets", "Doc.fltar"),
                original);

            string tocPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "TOCs", "Doc.fltoc"),
                "<CatapultToc />");

            TargetConfiguratorService service =
                new TargetConfiguratorService();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                service.ConfigureTarget(targetPath, tocPath, CreateProfile()));

            Assert.AreEqual(original, File.ReadAllText(targetPath));
            Assert.IsFalse(File.Exists(targetPath + ".bak"));
        }

        private AitDocumentProfile CreateProfile()
        {
            return new AitDocumentProfile
            {
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech.flpgl"
            };
        }

        private string WriteFile(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }
    }
}
