using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ATLASDocGenerator.Services.AitCleanup.IhmVariables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class IhmVariableReferenceTransformerTests
    {
        private const string MadCapNamespace =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

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
        public void Transform_ReplacesMenuStrSnippetAndIsIdempotent()
        {
            string projectRoot = Path.Combine(_temporaryDirectory, "FlareProject");
            string contentRoot = Path.Combine(projectRoot, "Content");
            Directory.CreateDirectory(contentRoot);
            Directory.CreateDirectory(Path.Combine(projectRoot, "Project"));

            string topicPath = Path.Combine(contentRoot, "topic.htm");
            File.WriteAllText(
                topicPath,
                "<html xmlns:MadCap=\"" + MadCapNamespace + "\"><body>"
                + "<MadCap:snippetText src=\"../Resources/Snippets/Topic21767.flsnp\" />"
                + "</body></html>",
                new UTF8Encoding(false));

            FrenchIhmVariableSetGenerationResult variables =
                new FrenchIhmVariableSetGenerationResult
                {
                    VariableSetName = "Menu_STR"
                };

            variables.TopicIdToVariableName.Add(
                "21767",
                "Abréviation");

            IhmVariableReferenceTransformer transformer =
                new IhmVariableReferenceTransformer();

            IhmVariableReferenceTransformResult firstResult =
                transformer.Transform(projectRoot, new[] { variables });

            Assert.AreEqual(1, firstResult.FilesModified);
            Assert.AreEqual(1, firstResult.ReferencesReplaced);
            Assert.AreEqual(0, firstResult.Errors.Count);

            XDocument transformed = XDocument.Load(topicPath);
            XElement variable = transformed
                .Descendants(XName.Get("variable", MadCapNamespace))
                .Single();

            Assert.AreEqual(
                "Menu_STR.Abréviation",
                variable.Attribute("name").Value);
            Assert.AreEqual("IHM", variable.Attribute("class").Value);

            Assert.AreEqual(
                1,
                Directory.GetFiles(
                    contentRoot,
                    "topic.htm.before-ihm-variables.*.bak").Length);

            IhmVariableReferenceTransformResult secondResult =
                transformer.Transform(projectRoot, new[] { variables });

            Assert.AreEqual(0, secondResult.FilesModified);
            Assert.AreEqual(0, secondResult.ReferencesReplaced);
            Assert.AreEqual(
                1,
                Directory.GetFiles(
                    contentRoot,
                    "topic.htm.before-ihm-variables.*.bak").Length);
        }

        [TestMethod]
        public void Transform_AlsoProcessesSnippetFiles()
        {
            string projectRoot = Path.Combine(_temporaryDirectory, "FlareProject");
            string snippets = Path.Combine(projectRoot, "Content", "Resources", "Snippets");
            Directory.CreateDirectory(snippets);
            Directory.CreateDirectory(Path.Combine(projectRoot, "Project"));
            string snippetPath = Path.Combine(snippets, "Parent.flsnp");
            File.WriteAllText(snippetPath,
                "<html xmlns:MadCap=\"" + MadCapNamespace + "\"><body>"
                + "<MadCap:snippetText src=\"Topic42.flsnp\" /></body></html>",
                new UTF8Encoding(false));

            FrenchIhmVariableSetGenerationResult variables =
                new FrenchIhmVariableSetGenerationResult { VariableSetName = "Menu_STR" };
            variables.TopicIdToVariableName.Add("42", "Confirmer");

            IhmVariableReferenceTransformResult result =
                new IhmVariableReferenceTransformer().Transform(projectRoot, new[] { variables });

            Assert.AreEqual(1, result.ReferencesReplaced);
            XElement variable = XDocument.Load(snippetPath)
                .Descendants(XName.Get("variable", MadCapNamespace)).Single();
            Assert.AreEqual("Menu_STR.Confirmer", (string)variable.Attribute("name"));
            Assert.AreEqual("IHM", (string)variable.Attribute("class"));
        }
    }
}
