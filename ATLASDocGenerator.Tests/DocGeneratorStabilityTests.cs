using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class DocGeneratorStabilityTests
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
        public void CreateDocumentFolder_WithParentProjectStructure_CreatesCompletePackage()
        {
            string projectRoot = CreateParentProjectFixture();
            DocGenerationRequest request = CreateRequest(projectRoot);

            GenerationResult result =
                new AtlasDocGenerationService().CreateDocumentFolder(request);

            Assert.AreEqual("DOC_001_Validation", result.FolderName);
            Assert.AreEqual(9, result.CreatedTopicPaths.Count);
            Assert.IsTrue(result.CreatedTopicPaths.All(File.Exists));
            Assert.IsTrue(File.Exists(result.TocPath));
            Assert.IsTrue(File.Exists(result.TargetPath));

            XDocument toc = XDocument.Load(result.TocPath);
            List<string> tocLinks = toc
                .Descendants("TocEntry")
                .Select(element => (string)element.Attribute("Link"))
                .ToList();

            CollectionAssert.Contains(
                tocLinks,
                "/Content/DOC_001_Validation/Title_DOC_001.htm");
            CollectionAssert.Contains(
                tocLinks,
                "/Content/DOC_001_Validation/Historique_DOC_001.htm");
            CollectionAssert.Contains(
                tocLinks,
                "/Content/Resources/Commun Stago/Divers/Sommaire.htm");

            XDocument target = XDocument.Load(result.TargetPath);
            Assert.AreEqual(
                "/Project/TOCs/DOC_001_Validation.fltoc",
                (string)target.Root.Attribute("MasterToc"));
            Assert.AreEqual(
                "/Content/Resources/Stylesheets/Styles_STA.css",
                (string)target.Root.Attribute("MasterStylesheet"));
            Assert.AreEqual(
                "STA TEST",
                GetTargetVariable(target, "General/dispositif"));
            Assert.AreEqual(
                "Guide de validation",
                GetTargetVariable(target, "General/GuideType"));
            Assert.IsFalse(new[] { target.Root }.Concat(target.Descendants())
                .SelectMany(element => element.Attributes())
                .Any(attribute => attribute.Name.LocalName.IndexOf("condition", StringComparison.OrdinalIgnoreCase) >= 0));
            Assert.IsFalse(target.Descendants()
                .Any(element => element.Name.LocalName.Equals("ConditionTagExpression", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(new[] { toc.Root }.Concat(toc.Descendants())
                .SelectMany(element => element.Attributes())
                .Any(attribute => attribute.Name.LocalName.Equals("conditions", StringComparison.OrdinalIgnoreCase)));

            XDocument firstChapter = XDocument.Load(
                Path.Combine(
                    result.DocumentFolderPath,
                    "1er_chapitre.htm"));
            Assert.AreEqual(
                "../Resources/Images/Logos/MonLogo.png",
                (string)firstChapter.Descendants("img").Single().Attribute("src"));
            Assert.IsFalse(result.CreatedTopicPaths
                .Select(XDocument.Load)
                .SelectMany(document => new[] { document.Root }.Concat(document.Descendants()))
                .SelectMany(element => element.Attributes())
                .Any(attribute => attribute.Name.LocalName.Equals("conditions", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void CreateDocumentFolder_Notice_UsesNoticeStructure()
        {
            string projectRoot = CreateParentProjectFixture();
            DocGenerationRequest request = CreateRequest(projectRoot);
            request.DocumentType = "Notice";

            GenerationResult result = new AtlasDocGenerationService().CreateDocumentFolder(request);

            Assert.AreEqual(7, result.CreatedTopicPaths.Count);
            Assert.IsFalse(result.CreatedTopicPaths.Any(path =>
                Path.GetFileName(path).StartsWith("Mesures_securite", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(result.CreatedTopicPaths.Any(path =>
                Path.GetFileName(path).StartsWith("Duree_inter", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(XDocument.Load(result.TocPath).Descendants("TocEntry").Any(entry =>
                ((string)entry.Attribute("Link") ?? string.Empty).EndsWith("/1er_chapitre.htm", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void CreateDocumentFolder_WhenTopicTemplateIsMissing_LeavesNoPartialArtifacts()
        {
            string projectRoot = CreateParentProjectFixture();
            File.Delete(Path.Combine(
                projectRoot,
                "Content",
                "Template_tech",
                "Prérequis.htm"));

            Assert.ThrowsExactly<FileNotFoundException>(() =>
                new AtlasDocGenerationService().CreateDocumentFolder(
                    CreateRequest(projectRoot)));

            AssertNoGeneratedArtifacts(projectRoot);
        }

        [TestMethod]
        public void CreateDocumentFolder_WhenTargetXmlIsInvalid_LeavesNoPartialArtifacts()
        {
            string projectRoot = CreateParentProjectFixture();
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Project",
                    "Targets",
                    "Doc_SAV.fltar"),
                "<CatapultTarget>");

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new AtlasDocGenerationService().CreateDocumentFolder(
                    CreateRequest(projectRoot)));

            AssertNoGeneratedArtifacts(projectRoot);
        }

        [TestMethod]
        public void CreateDocumentFolder_WhenRequiredStylesheetIsMissing_LeavesNoPartialArtifacts()
        {
            string projectRoot = CreateParentProjectFixture();
            File.Delete(Path.Combine(
                projectRoot,
                "Content",
                "Resources",
                "Stylesheets",
                "Styles_STA.css"));

            Assert.ThrowsExactly<FileNotFoundException>(() =>
                new AtlasDocGenerationService().CreateDocumentFolder(
                    CreateRequest(projectRoot)));

            AssertNoGeneratedArtifacts(projectRoot);
        }

        [TestMethod]
        public void CreateDocumentFolder_WithLegacyDocSavToc_RemainsCompatible()
        {
            string projectRoot = CreateParentProjectFixture();
            string tocFolder = Path.Combine(projectRoot, "Project", "TOCs");
            File.Move(
                Path.Combine(tocFolder, "PS_DR_tech.fltoc"),
                Path.Combine(tocFolder, "Doc_SAV.fltoc"));

            GenerationResult result =
                new AtlasDocGenerationService().CreateDocumentFolder(
                    CreateRequest(projectRoot));

            Assert.IsTrue(File.Exists(result.TocPath));
            Assert.IsTrue(File.Exists(result.TargetPath));
            Assert.AreEqual(9, result.CreatedTopicPaths.Count);
        }

        [TestMethod]
        public void CreateDocumentFolder_WithoutProjectTocOrTarget_UsesEmbeddedModels()
        {
            string projectRoot = CreateParentProjectFixture();
            File.Delete(Path.Combine(
                projectRoot,
                "Project",
                "TOCs",
                "PS_DR_tech.fltoc"));
            File.Delete(Path.Combine(
                projectRoot,
                "Project",
                "Targets",
                "Doc_SAV.fltar"));

            GenerationResult result =
                new AtlasDocGenerationService().CreateDocumentFolder(
                    CreateRequest(projectRoot));

            Assert.IsTrue(File.Exists(result.TocPath));
            Assert.IsTrue(File.Exists(result.TargetPath));
            Assert.AreEqual(9, result.CreatedTopicPaths.Count);

            XDocument target = XDocument.Load(result.TargetPath);
            Assert.AreEqual(
                "/Project/TOCs/DOC_001_Validation.fltoc",
                (string)target.Root.Attribute("MasterToc"));
        }

        [TestMethod]
        public void CreateDocumentFolder_WithCurrentTopicLocationAndEmbeddedToc_DoesNotRequireLegacyTemplateFolder()
        {
            string projectRoot = CreateParentProjectFixture();
            string legacyFolder = Path.Combine(
                projectRoot,
                "Content",
                "Template_tech");
            string currentFolder = Path.Combine(
                projectRoot,
                "Content",
                "Resources",
                "Commun Stago",
                "Topics_Tech");

            Directory.CreateDirectory(currentFolder);
            foreach (string legacyTopic in Directory.GetFiles(legacyFolder))
            {
                File.Move(
                    legacyTopic,
                    Path.Combine(currentFolder, Path.GetFileName(legacyTopic)));
            }
            Directory.Delete(legacyFolder);

            File.Delete(Path.Combine(
                projectRoot,
                "Project",
                "TOCs",
                "PS_DR_tech.fltoc"));
            File.Delete(Path.Combine(
                projectRoot,
                "Project",
                "Targets",
                "Doc_SAV.fltar"));

            GenerationResult result =
                new AtlasDocGenerationService().CreateDocumentFolder(
                    CreateRequest(projectRoot));

            Assert.AreEqual(9, result.CreatedTopicPaths.Count);
            Assert.IsTrue(result.CreatedTopicPaths.All(File.Exists));
            Assert.IsFalse(XDocument.Load(result.TocPath)
                .Descendants("TocEntry")
                .Select(entry => (string)entry.Attribute("Link") ?? string.Empty)
                .Any(link => link.IndexOf(
                    "/Template_tech/",
                    StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private string CreateParentProjectFixture()
        {
            string projectRoot = Path.Combine(
                _temporaryDirectory,
                "Source Projet Parent");

            WriteFile(
                Path.Combine(projectRoot, "Projet_Parent.flprj"),
                "<CatapultProject Version=\"1\" />");

            Dictionary<string, string> topics =
                new Dictionary<string, string>
                {
                    { "Title_doc.htm", Topic("Titre") },
                    { "Objectif.htm", Topic("Objectif") },
                    { "Mesures de sécurité.htm", Topic("Sécurité") },
                    { "Matériel nécessaire.htm", Topic("Matériel") },
                    { "Documents nécessaires.htm", Topic("Documents") },
                    { "Duree_inter_Remplacements.htm", Topic("Durée") },
                    { "Prérequis.htm", Topic("Prérequis") },
                    {
                        "1er_chapitre.htm",
                        TopicWithImage(
                            "Premier chapitre",
                            "../Resources/Resources/Images/Logos/MonLogo.png")
                    }
                };

            foreach (KeyValuePair<string, string> topic in topics)
            {
                WriteFile(
                    Path.Combine(
                        projectRoot,
                        "Content",
                        "Template_tech",
                        topic.Key),
                    topic.Value);
            }

            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Content",
                    "Resources",
                    "Commun Stago",
                    "topics_Tech",
                    "Historique_tech.htm"),
                Topic("Historique"));
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Content",
                    "Resources",
                    "Commun Stago",
                    "Divers",
                    "Sommaire.htm"),
                Topic("Sommaire"));
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Content",
                    "Resources",
                    "Images",
                    "Logos",
                    "MonLogo.png"),
                "image");
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Content",
                    "Resources",
                    "Stylesheets",
                    "Styles.css"),
                "body {} ");
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Content",
                    "Resources",
                    "Stylesheets",
                    "Styles_STA.css"),
                "body {} ");
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Content",
                    "Resources",
                    "PageLayouts",
                    "Tech.flpgl"),
                "<PageLayout />");
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Project",
                    "VariableSets",
                    "General.flvar"),
                "<CatapultVariableSet />");

            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Project",
                    "TOCs",
                    "PS_DR_tech.fltoc"),
                CreateParentToc());
            WriteFile(
                Path.Combine(
                    projectRoot,
                    "Project",
                    "Targets",
                    "Doc_SAV.fltar"),
                CreateParentTarget());

            return projectRoot;
        }

        private static DocGenerationRequest CreateRequest(string projectRoot)
        {
            return new DocGenerationRequest
            {
                ProjectRoot = projectRoot,
                DocumentType = "PS",
                ShortTitle = "Validation",
                DocumentReference = "DOC 001",
                Device = "STA TEST",
                Range = "STA",
                FullTitle = "Guide de validation"
            };
        }

        private static string CreateParentToc()
        {
            return "<CatapultToc Version=\"1\" conditions=\"Test.Condition\">"
                + TocEntry("/Content/Template_tech/Title_doc.htm")
                + TocEntry("/Content/Resources/Commun Stago/topics_Tech/Historique_tech.htm")
                + TocEntry("/Content/Template_tech/Objectif.htm")
                + TocEntry("/Content/Template_tech/Mesures de sécurité.htm")
                + TocEntry("/Content/Template_tech/Matériel nécessaire.htm")
                + TocEntry("/Content/Template_tech/Documents nécessaires.htm")
                + TocEntry("/Content/Template_tech/Prérequis.htm")
                + TocEntry("/Content/Resources/Commun Stago/Divers/Sommaire.htm")
                + TocEntry("/Content/Template_tech/1er_chapitre.htm")
                + "</CatapultToc>";
        }

        private static string CreateParentTarget()
        {
            return "<CatapultTarget Version=\"2\" Type=\"PDF\" conditions=\"Test.Condition\" "
                + "ConditionTagExpression=\"exclude[Test.Condition]\" "
                + "MasterToc=\"/Project/TOCs/Notice.fltoc\" "
                + "MasterStylesheet=\"/Content/Resources/Stylesheets/Styles.css\" "
                + "MasterPageLayout=\"/Content/Resources/PageLayouts/Tech.flpgl\">"
                + "<PrintedOutput GenerateIndexProxy=\"false\" GenerateGlossaryProxy=\"false\" />"
                + "<Variables><Variable Name=\"General/dispositif\">Ancien</Variable>"
                + "<Variable Name=\"General/GuideType\">Ancien</Variable>"
                + "<Variable Name=\"General/DocumentReference\">Ancien</Variable>"
                + "</Variables><ConditionTagExpression><Tag Name=\"Test.Condition\" Action=\"Exclude\" /></ConditionTagExpression></CatapultTarget>";
        }

        private static string Topic(string title)
        {
            return "<html xmlns:MadCap=\"http://www.madcapsoftware.com/Schemas/MadCap.xsd\" MadCap:conditions=\"Test.Condition\">"
                + "<head><title>" + title + "</title></head><body><p>" + title + "</p></body></html>";
        }

        private static string TopicWithImage(string title, string imagePath)
        {
            return "<html><head><title>" + title + "</title></head><body><p>"
                + title + "</p><img src=\"" + imagePath + "\" /></body></html>";
        }

        private static string TocEntry(string link)
        {
            return "<TocEntry Title=\"[%=System.LinkedTitle%]\" Link=\""
                + link + "\" />";
        }

        private static string GetTargetVariable(XDocument target, string name)
        {
            return target
                .Descendants("Variable")
                .Single(element =>
                    string.Equals(
                        (string)element.Attribute("Name"),
                        name,
                        StringComparison.OrdinalIgnoreCase))
                .Value;
        }

        private static void AssertNoGeneratedArtifacts(string projectRoot)
        {
            Assert.IsFalse(Directory.Exists(Path.Combine(
                projectRoot,
                "Content",
                "DOC_001_Validation")));
            Assert.IsFalse(File.Exists(Path.Combine(
                projectRoot,
                "Project",
                "TOCs",
                "DOC_001_Validation.fltoc")));
            Assert.IsFalse(File.Exists(Path.Combine(
                projectRoot,
                "Project",
                "Targets",
                "DOC_001_Validation.fltar")));
        }

        private static void WriteFile(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
}
