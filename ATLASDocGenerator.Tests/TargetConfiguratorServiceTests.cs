using System;
using System.IO;
using System.Linq;
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
                + "<CatapultTarget Type=\"PDF\"><PrintedOutput MasterToc=\"old-toc\" "
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
            Assert.AreEqual(
                "true",
                document.Root.Attribute("PatchHeadingLevels").Value);
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

        [TestMethod]
        public void ValidateTarget_ReportsDifferencesWithoutWriting()
        {
            string original =
                "<CatapultTarget Type=\"PDF\" MasterToc=\"old.fltoc\" "
                + "MasterStylesheet=\"old.css\" MasterPageLayout=\"old.flpgl\" "
                + "conditions=\"FPS.Device\" ConditionTagExpression=\"include[FPS.Device]\">"
                + "<Variables><Variable Name=\"General/DocumentReference\">DOC-1</Variable></Variables>"
                + "</CatapultTarget>";
            string targetPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "Targets", "Doc.fltar"),
                original);
            string tocPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "TOCs", "Doc.fltoc"),
                "<CatapultToc />");

            TargetValidationResult result = new TargetConfiguratorService()
                .ValidateTarget(targetPath, tocPath, CreateProfile());

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(4, result.Differences.Count);
            Assert.IsTrue(result.Differences.Any(difference =>
                difference.SettingName == "PatchHeadingLevels"
                && difference.ExpectedValue == "true"));
            Assert.AreEqual(original, File.ReadAllText(targetPath));
            Assert.IsFalse(File.Exists(targetPath + ".bak"));
        }

        [TestMethod]
        public void ConfigureTarget_PreservesConditionsAndVariables()
        {
            string targetPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "Targets", "Doc.fltar"),
                "<CatapultTarget Type=\"PDF\" MasterToc=\"old\" MasterStylesheet=\"old\" MasterPageLayout=\"old\" "
                + "conditions=\"FPS.Device\" ConditionTagExpression=\"include[FPS.Device]\">"
                + "<Variables><Variable Name=\"General/DocumentReference\">DOC-1</Variable></Variables>"
                + "</CatapultTarget>");
            string tocPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "TOCs", "Doc.fltoc"),
                "<CatapultToc />");

            new TargetConfiguratorService().ConfigureTarget(targetPath, tocPath, CreateProfile());

            XDocument target = XDocument.Load(targetPath);
            Assert.AreEqual("FPS.Device", (string)target.Root.Attribute("conditions"));
            Assert.AreEqual("include[FPS.Device]", (string)target.Root.Attribute("ConditionTagExpression"));
            Assert.AreEqual(
                "DOC-1",
                target.Descendants("Variable").Single().Value);
            Assert.AreEqual("true", (string)target.Root.Attribute("PatchHeadingLevels"));
        }

        [TestMethod]
        public void ConfigureTarget_DoesNotAddPrintHeadingOptionToHtmlTarget()
        {
            string targetPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "Targets", "Web.fltar"),
                "<CatapultTarget Type=\"WebHelp2\" MasterToc=\"old\" "
                + "MasterStylesheet=\"old\" MasterPageLayout=\"old\" />");
            string tocPath = WriteFile(
                Path.Combine(_temporaryDirectory, "Project", "TOCs", "Doc.fltoc"),
                "<CatapultToc />");

            new TargetConfiguratorService().ConfigureTarget(targetPath, tocPath, CreateProfile());

            XDocument target = XDocument.Load(targetPath);
            Assert.IsNull(target.Root.Attribute("PatchHeadingLevels"));
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
