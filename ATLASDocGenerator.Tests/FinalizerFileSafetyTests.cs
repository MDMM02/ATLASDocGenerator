using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitImportFinalizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class FinalizerFileSafetyTests
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
        public void CleanToc_RemovesConfiguredEntriesAndKeepsInitialBackup()
        {
            const string original =
                "<CatapultToc><TocEntry Title=\"A_HEADER\" Link=\"header.htm\" />"
                + "<TocEntry Title=\"Chapitre utile\" Link=\"chapter.htm\" />"
                + "</CatapultToc>";
            string tocPath = Path.Combine(_temporaryDirectory, "Manual.fltoc");
            File.WriteAllText(tocPath, original, new UTF8Encoding(false));

            AitDocumentProfile profile = new AitDocumentProfile
            {
                TocEntriesToRemove = new List<string> { "A_HEADER" }
            };
            TocCleanerService service = new TocCleanerService();

            List<string> firstResult = service.CleanToc(tocPath, profile);

            Assert.AreEqual(1, firstResult.Count);
            Assert.AreEqual("A_HEADER", firstResult[0]);
            Assert.AreEqual(original, File.ReadAllText(tocPath + ".bak", Encoding.UTF8));
            Assert.AreEqual(
                "Chapitre utile",
                (string)XDocument.Load(tocPath)
                    .Descendants("TocEntry")
                    .Single()
                    .Attribute("Title"));

            List<string> secondResult = service.CleanToc(tocPath, profile);
            Assert.AreEqual(0, secondResult.Count);
            Assert.AreEqual(original, File.ReadAllText(tocPath + ".bak", Encoding.UTF8));
        }

        [TestMethod]
        public void UpdateVariables_UpdatesValuesAndKeepsInitialBackup()
        {
            const string original =
                "<VariableSet><Variable Name=\"DocumentTitle\">Ancien titre</Variable>"
                + "<Variable Name=\"GuideType\">Ancien type</Variable></VariableSet>";
            string variableSetPath = Path.Combine(
                _temporaryDirectory,
                "Project",
                "VariableSets",
                "General.flvar");
            Directory.CreateDirectory(Path.GetDirectoryName(variableSetPath));
            File.WriteAllText(variableSetPath, original, new UTF8Encoding(false));

            AitImportFinalizerOptions options = new AitImportFinalizerOptions
            {
                DocumentType = AitDocumentType.UserNotice,
                DocumentTitle = "Nouveau titre",
                DeviceName = "STA R Max",
                DocumentReference = "DOC-123",
                DocumentIndex = "B",
                Language = "FR",
                MrefReference = "MREF-456"
            };
            VariableSetUpdaterService service = new VariableSetUpdaterService();

            service.UpdateGeneralVariables(_temporaryDirectory, options);

            Assert.AreEqual(original, File.ReadAllText(variableSetPath + ".bak", Encoding.UTF8));
            XDocument updated = XDocument.Load(variableSetPath);
            Assert.AreEqual("Nouveau titre", GetVariableValue(updated, "DocumentTitle"));
            Assert.AreEqual("Notice utilisateur", GetVariableValue(updated, "GuideType"));
            Assert.AreEqual("DOC-123", GetVariableValue(updated, "DocumentReference"));

            service.UpdateGeneralVariables(_temporaryDirectory, options);
            Assert.AreEqual(original, File.ReadAllText(variableSetPath + ".bak", Encoding.UTF8));
        }

        [TestMethod]
        public void UpdateVariables_ReferenceManual_DoesNotChangeMrefVariables()
        {
            const string original =
                "<VariableSet><Variable Name=\"Mref\">MREF-EXISTANTE</Variable>"
                + "<Variable Name=\"ReferenceMref\">REF-EXISTANTE</Variable></VariableSet>";
            string variableSetPath = Path.Combine(
                _temporaryDirectory, "Project", "VariableSets", "General.flvar");
            Directory.CreateDirectory(Path.GetDirectoryName(variableSetPath));
            File.WriteAllText(variableSetPath, original, new UTF8Encoding(false));

            new VariableSetUpdaterService().UpdateGeneralVariables(
                _temporaryDirectory,
                new AitImportFinalizerOptions
                {
                    DocumentType = AitDocumentType.ReferenceManual,
                    MrefReference = "NE-DOIT-PAS-ETRE-ECRITE"
                });

            XDocument updated = XDocument.Load(variableSetPath);
            Assert.AreEqual("MREF-EXISTANTE", GetVariableValue(updated, "Mref"));
            Assert.AreEqual("REF-EXISTANTE", GetVariableValue(updated, "ReferenceMref"));
        }

        [TestMethod]
        public void UpdateVariables_InvalidXmlDoesNotCreateBackup()
        {
            string variableSetPath = Path.Combine(
                _temporaryDirectory,
                "Project",
                "VariableSets",
                "General.flvar");
            Directory.CreateDirectory(Path.GetDirectoryName(variableSetPath));
            File.WriteAllText(variableSetPath, "<VariableSet>", new UTF8Encoding(false));

            VariableSetUpdaterService service = new VariableSetUpdaterService();

            Assert.ThrowsExactly<XmlException>(() =>
                service.UpdateGeneralVariables(
                    _temporaryDirectory,
                    new AitImportFinalizerOptions()));
            Assert.IsFalse(File.Exists(variableSetPath + ".bak"));
        }

        private static string GetVariableValue(XDocument document, string name)
        {
            XElement variable = document
                .Descendants()
                .Single(element =>
                    element.Name.LocalName == "Variable"
                    && string.Equals(
                        (string)element.Attribute("Name"),
                        name,
                        StringComparison.OrdinalIgnoreCase));

            XElement definition = variable.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "VariableDefinition");
            return definition == null ? variable.Value : definition.Value;
        }
    }
}
