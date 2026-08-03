using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services.AitCleanup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class SimpleStyleCleanupTransformerTests
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
        public void Transform_CreatesImmutableBackupAndIsIdempotent()
        {
            const string original =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<html><body><p class=\"a_normal_centered\">Texte</p></body></html>";

            string filePath = Path.Combine(_temporaryDirectory, "topic.htm");
            File.WriteAllText(filePath, original, new UTF8Encoding(false));

            SimpleStyleCleanupTransformer transformer =
                new SimpleStyleCleanupTransformer();

            CleanupReport firstReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, firstReport);

            string backupPath = filePath + ".before-ait-cleanup.bak";
            Assert.IsTrue(File.Exists(backupPath));
            Assert.AreEqual(original, File.ReadAllText(backupPath, Encoding.UTF8));
            Assert.AreEqual(1, firstReport.StylesCleaned);
            Assert.AreEqual(0, firstReport.Errors.Count);

            XDocument transformed = XDocument.Load(filePath);
            Assert.AreEqual(
                "a_centre",
                transformed.Root.Element("body").Element("p").Attribute("class").Value);

            CleanupReport secondReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, secondReport);

            Assert.AreEqual(0, secondReport.StylesCleaned);
            Assert.AreEqual(0, secondReport.Errors.Count);
            Assert.AreEqual(original, File.ReadAllText(backupPath, Encoding.UTF8));
        }
    }
}
