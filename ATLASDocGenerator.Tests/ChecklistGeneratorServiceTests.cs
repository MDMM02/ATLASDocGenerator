using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services.Checklist;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class ChecklistGeneratorServiceTests
    {
        private string temporaryDirectory;
        private string projectRoot;
        private string documentFolder;
        private string tocPath;

        [TestInitialize]
        public void CreateProject()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "ATLASDocGenerator.Tests", Guid.NewGuid().ToString("N"));
            projectRoot = Path.Combine(temporaryDirectory, "ProjectTest");
            documentFolder = Path.Combine(projectRoot, "Content", "Document");
            tocPath = Path.Combine(projectRoot, "Project", "TOCs", "Document.fltoc");
            Directory.CreateDirectory(documentFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(tocPath));

            string qiqoFolder = Path.Combine(projectRoot, "Content", "Resources", "Commun Stago", "QIQO_content");
            Directory.CreateDirectory(qiqoFolder);
            File.WriteAllText(Path.Combine(qiqoFolder, "QIQO_table.flsnp"),
                "<html><body><table /></body></html>", new UTF8Encoding(false));
        }

        [TestCleanup]
        public void DeleteProject()
        {
            if (Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, true);
        }

        [TestMethod]
        public void GenerateChecklistFromFile_UsesEveryH1InDocumentToc()
        {
            string first = WriteTopic("Premier.htm", "<html><body><h1>Première étape</h1></body></html>");
            WriteTopic("Second.htm", "<html><body><h1>Deuxième étape</h1><h1>Troisième étape</h1></body></html>");
            WriteToc("Premier.htm", "Second.htm");
            byte[] firstOriginal = File.ReadAllBytes(first);

            ChecklistGeneratorService service = new ChecklistGeneratorService();
            int count = service.GenerateChecklistFromFile(first);

            Assert.AreEqual(3, count);
            CollectionAssert.AreEqual(firstOriginal, File.ReadAllBytes(first));
            Assert.AreEqual(Path.Combine(documentFolder, "Checklist.htm"), service.LastGeneratedFilePath);

            XDocument checklist = XDocument.Load(service.LastGeneratedFilePath);
            string[] labels = checklist.Descendants("p")
                .Where(element => !element.Elements().Any(child => child.Name.LocalName == "snippetText"))
                .Select(element => element.Value).ToArray();
            CollectionAssert.AreEqual(
                new[] { "Première étape", "Deuxième étape", "Troisième étape" },
                labels);
            Assert.AreEqual(3, checklist.Descendants().Count(element => element.Name.LocalName == "snippetBlock"));
            Assert.AreEqual(2, checklist.Descendants().Count(element => element.Name.LocalName == "snippetText"));
            XElement numberedActions = checklist.Descendants("ol").Single();
            Assert.AreEqual("Action_num", (string)numberedActions.Attribute("class"));
            Assert.AreEqual(3, numberedActions.Elements("li").Count());
            Assert.IsTrue(File.Exists(Path.Combine(
                projectRoot, "Content", "Resources", "Commun Stago", "QIQO_content", "intro_checklist.flsnp")));
            Assert.IsTrue(File.Exists(Path.Combine(
                projectRoot, "Content", "Resources", "Commun Stago", "QIQO_content", "titre_checklist.flsnp")));

            XDocument toc = XDocument.Load(tocPath);
            Assert.AreEqual(1, toc.Descendants("TocEntry").Count(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).EndsWith("/Checklist.htm", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(File.Exists(tocPath + ".before-checklist.bak"));
        }

        [TestMethod]
        public void GenerateChecklistFromFile_RunTwice_ReplacesTopicWithoutDuplicatingTocEntry()
        {
            string first = WriteTopic("Premier.htm", "<html><body><h1>Chapitre A</h1></body></html>");
            WriteToc("Premier.htm");
            ChecklistGeneratorService service = new ChecklistGeneratorService();

            service.GenerateChecklistFromFile(first);
            int count = service.GenerateChecklistFromFile(first);

            Assert.AreEqual(1, count);
            Assert.IsTrue(File.Exists(Path.Combine(documentFolder, "Checklist.htm.before-checklist.bak")));
            XDocument toc = XDocument.Load(tocPath);
            Assert.AreEqual(1, toc.Descendants("TocEntry").Count(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).EndsWith("/Checklist.htm", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void GenerateChecklistFromFile_WithoutH1_DoesNotCreateChecklist()
        {
            string first = WriteTopic("Premier.htm", "<html><body><h2>Sous-section</h2></body></html>");
            WriteToc("Premier.htm");

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                new ChecklistGeneratorService().GenerateChecklistFromFile(first));
            Assert.IsFalse(File.Exists(Path.Combine(documentFolder, "Checklist.htm")));
        }

        [TestMethod]
        public void GenerateChecklistFromFile_WithInvalidTopicXml_DoesNotCreateChecklist()
        {
            string first = WriteTopic("Premier.htm", "<html><body><h1>Chapitre</body>");
            WriteToc("Premier.htm");

            Assert.ThrowsExactly<System.Xml.XmlException>(() =>
                new ChecklistGeneratorService().GenerateChecklistFromFile(first));
            Assert.IsFalse(File.Exists(Path.Combine(documentFolder, "Checklist.htm")));
        }

        [TestMethod]
        public void GenerateChecklist_ExcludesNonNumberedH1ButIncludesPrerequisites()
        {
            WriteTopic("Introduction.htm", "<html><body><h1 class=\"no_num\">Objectif</h1><h1>Action utile</h1></body></html>");
            string prerequisite = WriteTopic(
                "Prérequis.htm",
                "<html><body><h1 class=\"non_numerote\">Prérequis</h1>"
                + "<p class=\"ss_section\">Vérifications avec le destinataire</p></body></html>");
            WriteToc("Introduction.htm", "Prérequis.htm");

            int count = new ChecklistGeneratorService().GenerateChecklistFromFile(prerequisite);

            Assert.AreEqual(2, count);
            XDocument checklist = XDocument.Load(Path.Combine(documentFolder, "Checklist.htm"));
            string combined = string.Join("|", checklist.Descendants("p").Select(element => element.Value));
            StringAssert.Contains(combined, "Action utile");
            StringAssert.Contains(combined, "Vérifications avec le destinataire");
            Assert.IsFalse(combined.Contains("Prérequis"));
            Assert.IsFalse(combined.Contains("Objectif"));
        }

        [TestMethod]
        public void GetAvailableTargets_FindsTargetsInSubfolders()
        {
            WriteTopic("Premier.htm", "<html><body><h1>Action</h1></body></html>");
            WriteToc("Premier.htm");
            string targetPath = WriteTarget(Path.Combine("Instrument", "Document.fltar"),
                "/Project/TOCs/Document.fltoc", "DOC-001");

            ChecklistTargetInfo target = new ChecklistGeneratorService()
                .GetAvailableTargets(projectRoot).Single();

            Assert.AreEqual(targetPath, target.TargetPath);
            Assert.AreEqual("DOC-001", target.DocumentReference);
            StringAssert.Contains(target.DisplayName, "Instrument");
        }

        [TestMethod]
        public void Generate_NewDocument_DuplicatesTargetAndOnlyChangesDocumentReferenceAndToc()
        {
            WriteTopic("Premier.htm", "<html><body><h1>Action</h1></body></html>");
            WriteToc("Premier.htm");
            string sourceTargetPath = WriteTarget(Path.Combine("Instrument", "Document.fltar"),
                "/Project/TOCs/Document.fltoc", "DOC-001");

            ChecklistGenerationResult result = new ChecklistGeneratorService().Generate(
                new ChecklistGenerationRequest
                {
                    ProjectRoot = projectRoot,
                    SourceTargetPath = sourceTargetPath,
                    CreateNewDocument = true,
                    NewDocumentReference = "CHK 123"
                });

            Assert.IsTrue(File.Exists(result.ChecklistTopicPath));
            Assert.IsTrue(File.Exists(result.TocPath));
            Assert.IsTrue(File.Exists(result.TargetPath));
            StringAssert.EndsWith(result.TargetPath, Path.Combine("Instrument", "CHK_123_checklist.fltar"));

            XDocument target = XDocument.Load(result.TargetPath);
            Assert.AreEqual("FPS.sthemO301", (string)target.Root.Attribute("conditions"));
            Assert.AreEqual("include[FPS.sthemO301]", (string)target.Root.Attribute("ConditionTagExpression"));
            Assert.AreEqual("CHK 123", GetTargetVariable(target, "General/DocumentReference"));
            Assert.AreEqual("Conservée", GetTargetVariable(target, "General/AutreVariable"));
            StringAssert.EndsWith((string)target.Root.Attribute("MasterToc"), "/CHK_123_checklist.fltoc");

            XDocument toc = XDocument.Load(result.TocPath);
            Assert.AreEqual(2, toc.Descendants("TocEntry").Count());
            Assert.IsFalse(toc.Descendants("TocEntry").Any(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).Contains("Premier.htm")));
            Assert.AreEqual(2, toc.Descendants("TocEntry").Count(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).Contains("Checklist.htm")));
            Assert.IsTrue(toc.Descendants("TocEntry").Any(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).EndsWith("#checklist-step-001", StringComparison.OrdinalIgnoreCase)
                && ((string)entry.Attribute("Title") ?? string.Empty).StartsWith("1. ", StringComparison.Ordinal)));
            Assert.AreEqual(
                "checklist-step-001",
                (string)XDocument.Load(result.ChecklistTopicPath).Descendants("li").Single().Attribute("id"));
        }

        [TestMethod]
        public void Generate_NewDocument_ExcludesSummaryFromChecklistAndStandaloneToc()
        {
            WriteTopic("Premier.htm", "<html><body><h1>Action</h1></body></html>");
            WriteTopic("Sommaire.htm", "<html><body><h1>Sommaire</h1></body></html>");
            WriteToc("Sommaire.htm", "Premier.htm");
            string sourceTargetPath = WriteTarget("Document.fltar",
                "/Project/TOCs/Document.fltoc", "DOC-001");

            ChecklistGenerationResult result = new ChecklistGeneratorService().Generate(
                new ChecklistGenerationRequest
                {
                    ProjectRoot = projectRoot,
                    SourceTargetPath = sourceTargetPath,
                    CreateNewDocument = true,
                    NewDocumentReference = "CHK-002"
                });

            string checklistText = XDocument.Load(result.ChecklistTopicPath).Root.Value;
            Assert.IsFalse(checklistText.Contains("Sommaire"));
            Assert.IsTrue(checklistText.Contains("Action"));
            XDocument toc = XDocument.Load(result.TocPath);
            Assert.IsFalse(toc.Descendants("TocEntry").Any(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).Contains("Sommaire.htm")));
            Assert.IsFalse(toc.Descendants("TocEntry").Any(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).Contains("Premier.htm")));
            Assert.IsTrue(toc.Descendants("TocEntry").All(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).Contains("Checklist.htm")));
        }

        private string WriteTopic(string fileName, string xml)
        {
            string path = Path.Combine(documentFolder, fileName);
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            return path;
        }

        private void WriteToc(params string[] topicNames)
        {
            XElement root = new XElement("CatapultToc", topicNames.Select(name =>
                new XElement("TocEntry", new XAttribute("Link", "/Content/Document/" + name))));
            new XDocument(root).Save(tocPath);
        }

        private string WriteTarget(string relativePath, string masterToc, string documentReference)
        {
            string path = Path.Combine(projectRoot, "Project", "Targets", relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            XDocument target = new XDocument(
                new XElement("CatapultTarget",
                    new XAttribute("MasterToc", masterToc),
                    new XAttribute("conditions", "FPS.sthemO301"),
                    new XAttribute("ConditionTagExpression", "include[FPS.sthemO301]"),
                    new XElement("Variables",
                        new XElement("Variable", new XAttribute("Name", "General/DocumentReference"), documentReference),
                        new XElement("Variable", new XAttribute("Name", "General/AutreVariable"), "Conservée"))));
            target.Save(path);
            return path;
        }

        private string GetTargetVariable(XDocument target, string name)
        {
            return target.Descendants("Variable").Single(variable =>
                string.Equals((string)variable.Attribute("Name"), name, StringComparison.OrdinalIgnoreCase)).Value;
        }
    }
}
