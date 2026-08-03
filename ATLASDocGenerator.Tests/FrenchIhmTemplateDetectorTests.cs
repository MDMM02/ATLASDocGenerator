using System;
using System.IO;
using System.Linq;
using System.Text;
using ATLASDocGenerator.Services.AitCleanup.IhmVariables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class FrenchIhmTemplateDetectorTests
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
        public void Detect_FindsUsedFrenchMenuStrTemplate()
        {
            string xmlPath = WriteXml(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<AuthorIT xmlns=\"http://www.authorit.com/xml/authorit\"><Objects>"
                + Topic("18564", "Menu_STR", "0", "true", "1", "-1")
                + Topic("21767", "Abréviation", "18564", "false", "1", "-1")
                + Topic("30000", "Menu_STR anglais", "18564", "false", "13", "21767")
                + "</Objects></AuthorIT>");

            FrenchIhmTemplateDetector detector =
                new FrenchIhmTemplateDetector();

            FrenchIhmTemplateInfo result = detector
                .Detect(xmlPath)
                .Single();

            Assert.AreEqual("18564", result.Id);
            Assert.AreEqual("Menu_STR", result.Description);
            Assert.AreEqual(1, result.FrenchTopicCount);
        }

        [TestMethod]
        public void Detect_DoesNotReturnUnusedFrenchTemplate()
        {
            string xmlPath = WriteXml(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<AuthorIT xmlns=\"http://www.authorit.com/xml/authorit\"><Objects>"
                + Topic("18564", "Menu_STR", "0", "true", "1", "-1")
                + "</Objects></AuthorIT>");

            FrenchIhmTemplateDetector detector =
                new FrenchIhmTemplateDetector();

            Assert.AreEqual(0, detector.Detect(xmlPath).Count);
        }

        private string WriteXml(string content)
        {
            string path = Path.Combine(_temporaryDirectory, "authorit.xml");
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        private static string Topic(
            string id,
            string description,
            string basedOn,
            string isTemplate,
            string locId,
            string variantParentId)
        {
            return "<Topic><Object>"
                + "<BasedOn>" + basedOn + "</BasedOn>"
                + "<Description>" + description + "</Description>"
                + "<ID>" + id + "</ID>"
                + "<IsTemplate>" + isTemplate + "</IsTemplate>"
                + "<Type>Topic</Type>"
                + "<VariantParentID>" + variantParentId + "</VariantParentID>"
                + "<LocID>" + locId + "</LocID>"
                + "</Object><Text /></Topic>";
        }
    }
}
