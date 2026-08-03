using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services.AitCleanup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class AitCleanupTransformerSafetyTests
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
        public void ActionResultTransform_CreatesBackupAndIsIdempotent()
        {
            const string original =
                "<html><body><p class=\"a_action_num\">Action</p>"
                + "<p class=\"a_resultat\">Résultat</p></body></html>";
            string filePath = WriteTopic("action-result.htm", original);

            ActionResultListTransformer transformer =
                new ActionResultListTransformer();
            CleanupReport firstReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, firstReport);

            AssertInitialBackup(filePath, original);
            Assert.AreEqual(1, firstReport.ActionResultListsTransformed);
            Assert.AreEqual(0, firstReport.Errors.Count);
            Assert.IsTrue(XDocument.Load(filePath)
                .Descendants("ol")
                .Any(element => (string)element.Attribute("class") == "Action_num"));

            string transformed = File.ReadAllText(filePath, Encoding.UTF8);
            CleanupReport secondReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, secondReport);

            Assert.AreEqual(0, secondReport.ActionResultListsTransformed);
            Assert.AreEqual(transformed, File.ReadAllText(filePath, Encoding.UTF8));
            AssertInitialBackup(filePath, original);
        }

        [TestMethod]
        public void BulletListTransform_CreatesBackupAndIsIdempotent()
        {
            const string original =
                "<html><body><p>Introduction</p><p class=\"a_tiret\">Premier</p>"
                + "<p class=\"a_tiret_retrait_2\">Second</p></body></html>";
            string filePath = WriteTopic("bullets.htm", original);

            BulletListTransformer transformer = new BulletListTransformer();
            CleanupReport firstReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, firstReport);

            AssertInitialBackup(filePath, original);
            Assert.AreEqual(1, firstReport.BulletListsTransformed);
            Assert.AreEqual(0, firstReport.Errors.Count);
            Assert.IsTrue(XDocument.Load(filePath).Descendants("ul").Any());

            string transformed = File.ReadAllText(filePath, Encoding.UTF8);
            CleanupReport secondReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, secondReport);

            Assert.AreEqual(0, secondReport.BulletListsTransformed);
            Assert.AreEqual(transformed, File.ReadAllText(filePath, Encoding.UTF8));
            AssertInitialBackup(filePath, original);
        }

        [TestMethod]
        public void CalloutTransform_CreatesBackupAndIsIdempotent()
        {
            const string original =
                "<html><body><table><tr><td><img src=\"information.png\" /></td>"
                + "<td><p>Information importante</p></td></tr></table></body></html>";
            string filePath = WriteTopic("callout.htm", original);

            CalloutTransformer transformer = new CalloutTransformer();
            CleanupReport firstReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, firstReport);

            AssertInitialBackup(filePath, original);
            Assert.AreEqual(1, firstReport.CalloutsTransformed);
            Assert.AreEqual(0, firstReport.Errors.Count);
            Assert.IsTrue(XDocument.Load(filePath)
                .Descendants("div")
                .Any(element => (string)element.Attribute("class") == "a_Information"));

            string transformed = File.ReadAllText(filePath, Encoding.UTF8);
            CleanupReport secondReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, secondReport);

            Assert.AreEqual(0, secondReport.CalloutsTransformed);
            Assert.AreEqual(transformed, File.ReadAllText(filePath, Encoding.UTF8));
            AssertInitialBackup(filePath, original);
        }

        [TestMethod]
        public void FigureTransform_CreatesBackupAndIsIdempotent()
        {
            const string original =
                "<html><body><p class=\"a_figure\">Légende</p>"
                + "<p class=\"a_normal_centered\"><img src=\"figure.png\" /></p>"
                + "</body></html>";
            string filePath = WriteTopic("figure.htm", original);

            FigureTransformer transformer = new FigureTransformer();
            CleanupReport firstReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, firstReport);

            AssertInitialBackup(filePath, original);
            Assert.AreEqual(1, firstReport.FiguresTransformed);
            Assert.AreEqual(0, firstReport.Errors.Count);
            Assert.IsTrue(XDocument.Load(filePath)
                .Descendants("div")
                .Any(element => (string)element.Attribute("class") == "a_figure"));

            string transformed = File.ReadAllText(filePath, Encoding.UTF8);
            CleanupReport secondReport = new CleanupReport();
            transformer.Transform(new[] { filePath }, secondReport);

            Assert.AreEqual(0, secondReport.FiguresTransformed);
            Assert.AreEqual(transformed, File.ReadAllText(filePath, Encoding.UTF8));
            AssertInitialBackup(filePath, original);
        }

        private string WriteTopic(string fileName, string content)
        {
            string filePath = Path.Combine(_temporaryDirectory, fileName);
            File.WriteAllText(filePath, content, new UTF8Encoding(false));
            return filePath;
        }

        private static void AssertInitialBackup(string filePath, string original)
        {
            string backupPath = filePath + ".before-ait-cleanup.bak";
            Assert.IsTrue(File.Exists(backupPath));
            Assert.AreEqual(original, File.ReadAllText(backupPath, Encoding.UTF8));
        }
    }
}
