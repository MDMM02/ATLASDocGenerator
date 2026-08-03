using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Services.Checklist;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class ChecklistGeneratorServiceTests
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
        public void GenerateChecklistFromFile_CreatesOneItemPerH1AndBackup()
        {
            string path = WriteTopic(
                "<html><head><title>Test</title></head><body>"
                + "<h1> Première   étape </h1><p>Texte conservé</p>"
                + "<h1>Deuxième <span>étape</span></h1>"
                + "</body></html>");
            byte[] originalBytes = File.ReadAllBytes(path);
            ChecklistGeneratorService service =
                new ChecklistGeneratorService();

            int count = service.GenerateChecklistFromFile(path);

            Assert.AreEqual(2, count);
            Assert.AreEqual(path, service.LastGeneratedFilePath);
            CollectionAssert.AreEqual(
                originalBytes,
                File.ReadAllBytes(path + ".atlas-checklist.bak"));

            XDocument document = XDocument.Load(path);
            XElement checklist = GetChecklists(document).Single();
            XElement[] items = checklist
                .Elements()
                .Where(element => HasClass(element, "atlas-checklist-item"))
                .ToArray();

            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Première étape", items[0].Elements("p").First().Value);
            Assert.AreEqual("Deuxième étape", items[1].Elements("p").First().Value);
            Assert.IsTrue(document.Descendants("p").Any(element =>
                element.Value == "Texte conservé"));

            byte[] generatedBytes = File.ReadAllBytes(path);
            Assert.IsFalse(
                generatedBytes.Length >= 3
                && generatedBytes[0] == 0xEF
                && generatedBytes[1] == 0xBB
                && generatedBytes[2] == 0xBF);
        }

        [TestMethod]
        public void GenerateChecklistFromFile_RunTwice_ReplacesInsteadOfDuplicating()
        {
            string path = WriteTopic(
                "<html><body><h1>Chapitre A</h1><h1>Chapitre B</h1></body></html>");
            ChecklistGeneratorService service =
                new ChecklistGeneratorService();

            service.GenerateChecklistFromFile(path);
            int count = service.GenerateChecklistFromFile(path);

            XDocument document = XDocument.Load(path);
            Assert.AreEqual(2, count);
            Assert.AreEqual(1, GetChecklists(document).Count());
            Assert.AreEqual(
                2,
                document.Descendants().Count(element =>
                    HasClass(element, "atlas-checklist-item")));
        }

        [TestMethod]
        public void GenerateChecklistFromFile_WithoutH1_LeavesTopicUntouched()
        {
            string path = WriteTopic(
                "<html><body><h2>Sous-section</h2><p>Texte</p></body></html>");
            byte[] originalBytes = File.ReadAllBytes(path);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                new ChecklistGeneratorService().GenerateChecklistFromFile(path));

            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(path));
            Assert.IsFalse(File.Exists(path + ".atlas-checklist.bak"));
        }

        [TestMethod]
        public void GenerateChecklistFromFile_WithInvalidXml_LeavesTopicUntouched()
        {
            string path = WriteTopic("<html><body><h1>Chapitre</body>");
            byte[] originalBytes = File.ReadAllBytes(path);

            Assert.ThrowsExactly<System.Xml.XmlException>(() =>
                new ChecklistGeneratorService().GenerateChecklistFromFile(path));

            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(path));
            Assert.IsFalse(File.Exists(path + ".atlas-checklist.bak"));
        }

        private string WriteTopic(string xml)
        {
            string path = Path.Combine(_temporaryDirectory, "Checklist.htm");
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            return path;
        }

        private static System.Collections.Generic.IEnumerable<XElement>
            GetChecklists(XDocument document)
        {
            return document.Descendants().Where(element =>
                HasClass(element, "atlas-checklist"));
        }

        private static bool HasClass(XElement element, string className)
        {
            XAttribute attribute = element.Attribute("class");
            if (attribute == null)
            {
                return false;
            }

            return attribute.Value
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Contains(className);
        }
    }
}
