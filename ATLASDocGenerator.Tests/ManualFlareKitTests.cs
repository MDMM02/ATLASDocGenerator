using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitCleanup;
using ATLASDocGenerator.Services.AitCleanup.IhmVariables;
using ATLASDocGenerator.Services.AitImportFinalizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class ManualFlareKitTests
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
        public void KitFixture_ExercisesCleanupAndIhmTwiceWithoutRegression()
        {
            string kitRoot = ExtractPristineKit();
            string projectRoot = Path.Combine(kitRoot, "Projet_Test_ATLAS");

            string sourceXml = Path.Combine(kitRoot, "AuthorIt_Test_IHM.xml");
            FrenchIhmTemplateInfo template =
                new FrenchIhmTemplateDetector().Detect(sourceXml).Single();

            Assert.AreEqual("18564", template.Id);
            Assert.AreEqual("Menu_STR", template.Description);
            Assert.AreEqual(2, template.FrenchTopicCount);

            FrenchIhmVariableSetGenerationResult variables =
                new FrenchIhmVariableSetGenerator().Generate(
                    sourceXml,
                    projectRoot,
                    template.Id);

            Assert.AreEqual(2, variables.VariablesGenerated);

            IhmVariableReferenceTransformResult ihmResult =
                new IhmVariableReferenceTransformer().Transform(
                    projectRoot,
                    new[] { variables });

            Assert.AreEqual(2, ihmResult.ReferencesReplaced);
            Assert.AreEqual(0, ihmResult.Errors.Count);

            string topicPath = Path.Combine(
                projectRoot,
                "Content",
                "Tests",
                "AIT_Transformations.htm");

            CleanupReport firstReport = new CleanupReport();
            new CalloutTransformer().Transform(new[] { topicPath }, firstReport);
            new FigureTransformer().Transform(new[] { topicPath }, firstReport);
            new ActionResultListTransformer().Transform(new[] { topicPath }, firstReport);
            new BulletListTransformer().Transform(new[] { topicPath }, firstReport);
            new SimpleStyleCleanupTransformer().Transform(new[] { topicPath }, firstReport);

            Assert.AreEqual(1, firstReport.CalloutsTransformed);
            Assert.AreEqual(1, firstReport.FiguresTransformed);
            Assert.AreEqual(1, firstReport.ActionResultListsTransformed);
            Assert.AreEqual(1, firstReport.BulletListsTransformed);
            Assert.IsTrue(firstReport.StylesCleaned >= 4);
            Assert.AreEqual(0, firstReport.Errors.Count);
            Assert.IsTrue(File.Exists(
                topicPath + ".before-ait-cleanup.bak"));

            CleanupReport secondReport = new CleanupReport();
            new CalloutTransformer().Transform(new[] { topicPath }, secondReport);
            new FigureTransformer().Transform(new[] { topicPath }, secondReport);
            new ActionResultListTransformer().Transform(new[] { topicPath }, secondReport);
            new BulletListTransformer().Transform(new[] { topicPath }, secondReport);
            new SimpleStyleCleanupTransformer().Transform(new[] { topicPath }, secondReport);

            Assert.AreEqual(0, secondReport.CalloutsTransformed);
            Assert.AreEqual(0, secondReport.FiguresTransformed);
            Assert.AreEqual(0, secondReport.ActionResultListsTransformed);
            Assert.AreEqual(0, secondReport.BulletListsTransformed);
            Assert.AreEqual(0, secondReport.StylesCleaned);
            Assert.AreEqual(0, secondReport.Errors.Count);
        }

        [TestMethod]
        public void KitFixture_ExercisesFinalizerWithRealResourcePackage()
        {
            string kitRoot = ExtractPristineKit();
            string projectRoot = Path.Combine(kitRoot, "Projet_Test_ATLAS");

            string repositoryRoot =
                Directory.GetParent(FindKitArchive()).FullName;
            string resourceRoot = Path.Combine(
                repositoryRoot,
                "ATLASDocGenerator",
                "Templates");
            string tocPath = Path.Combine(
                projectRoot,
                "Project",
                "TOCs",
                "Test_Finalizer.fltoc");
            string targetPath = Path.Combine(
                projectRoot,
                "Project",
                "Targets",
                "Test_Finalizer_PDF.fltar");

            AitDocumentProfile profile = new AitDocumentProfile
            {
                DocumentType = AitDocumentType.TechnicalDocument,
                DisplayName = "Document technique",
                PrimaryStylesheet = "Resources/Stylesheets/Styles.css",
                PrimaryPageLayout = "Resources/PageLayouts/Tech.flpgl",
                TocEntriesToRemove = new List<string>
                {
                    "A_HEADER",
                    "A_FOOTER",
                    "Table des matières",
                    "Cover"
                }
            };

            List<string> removedEntries =
                new TocCleanerService().CleanToc(tocPath, profile);
            Assert.AreEqual(4, removedEntries.Count);

            ResourceCopyService resourceService =
                new ResourceCopyService(resourceRoot);
            ResourceCopyResult firstCopy =
                resourceService.CopyResources(projectRoot, profile);

            Assert.IsTrue(firstCopy.FilesCopied > 0);
            Assert.AreEqual(2, firstCopy.FilesUpdated);
            Assert.AreEqual(2, firstCopy.BackupsCreated);

            AitImportFinalizerOptions options =
                new AitImportFinalizerOptions
                {
                    DocumentType = AitDocumentType.TechnicalDocument,
                    DocumentTitle = "Guide de validation ATLAS",
                    DeviceName = "STA TEST",
                    DocumentReference = "DOC-TEST-001",
                    DocumentIndex = "B",
                    Language = "FR",
                    MrefReference = "MREF-TEST-001"
                };

            new VariableSetUpdaterService().UpdateGeneralVariables(
                projectRoot,
                options);
            new TargetConfiguratorService().ConfigureTarget(
                targetPath,
                tocPath,
                profile);

            string generalPath = Path.Combine(
                projectRoot,
                "Project",
                "VariableSets",
                "General.flvar");
            XDocument general = XDocument.Load(generalPath);

            Assert.IsTrue(HasVariableValue(
                general,
                "DocumentTitle",
                "Guide de validation ATLAS"));
            Assert.IsTrue(HasVariableValue(
                general,
                "DocumentReference",
                "DOC-TEST-001"));
            Assert.IsTrue(File.Exists(generalPath + ".bak"));
            Assert.IsTrue(File.Exists(
                generalPath + ".before-ait-finalizer.bak"));

            XDocument target = XDocument.Load(targetPath);
            Assert.AreEqual(
                "/Content/Resources/Stylesheets/Styles.css",
                target.Root.Attribute("MasterStylesheet").Value);
            Assert.AreEqual(
                "/Content/Resources/PageLayouts/Tech.flpgl",
                target.Root.Attribute("MasterPageLayout").Value);
            Assert.IsTrue(File.Exists(targetPath + ".bak"));

            ResourceCopyResult secondCopy =
                resourceService.CopyResources(projectRoot, profile);
            Assert.AreEqual(0, secondCopy.FilesCopied);
            Assert.AreEqual(0, secondCopy.FilesUpdated);
            Assert.AreEqual(0, secondCopy.BackupsCreated);
            Assert.AreEqual(1, secondCopy.FilesPreserved);
            Assert.IsTrue(secondCopy.FilesUnchanged > 0);
            Assert.AreEqual(
                0,
                new TocCleanerService().CleanToc(tocPath, profile).Count);
        }

        private static bool HasVariableValue(
            XDocument document,
            string name,
            string value)
        {
            return document
                .Descendants()
                .Where(element => element.Name.LocalName == "Variable")
                .Any(element =>
                    string.Equals(
                        (string)element.Attribute("Name"),
                        name,
                        StringComparison.OrdinalIgnoreCase)
                    && element.Value == value);
        }

        private string ExtractPristineKit()
        {
            string extractionRoot = Path.Combine(
                _temporaryDirectory,
                "ExtractedKit");

            ZipFile.ExtractToDirectory(
                FindKitArchive(),
                extractionRoot);

            return Path.Combine(
                extractionRoot,
                "Kit_Test_Flare");
        }

        private static string FindKitArchive()
        {
            string workingDirectoryCandidate = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Kit_Test_Flare.zip");

            if (File.Exists(workingDirectoryCandidate))
            {
                return workingDirectoryCandidate;
            }

            DirectoryInfo directory =
                new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                string candidate = Path.Combine(
                    directory.FullName,
                    "Kit_Test_Flare.zip");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Le fichier Kit_Test_Flare.zip est introuvable depuis le projet de tests.");
        }
    }
}
