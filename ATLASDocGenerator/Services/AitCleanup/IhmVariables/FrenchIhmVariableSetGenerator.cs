using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services.AitCleanup.IhmVariables
{
    /// <summary>
    /// Génère un fichier de variables MadCap Flare (.flvar)
    /// à partir des topics français Author-it basés sur un template sélectionné.
    ///
    /// Le template n'est jamais fixé dans le code :
    /// son ID est transmis par le formulaire après détection dans le XML.
    /// </summary>
    public class FrenchIhmVariableSetGenerator
    {
        private const string FrenchLocId = "1";

        // Marqueur temporaire utilisé pour conserver les balises Author-it <nbs/>.
        private const string NonBreakingSpaceMarker = "\uE000";

        /// <summary>
        /// Génère un fichier .flvar pour un template Author-it sélectionné.
        /// </summary>
        /// <param name="sourceXmlPath">
        /// Chemin du fichier XML exporté depuis Author-it.
        /// </param>
        /// <param name="selectedPath">
        /// Chemin sélectionné dans la popup :
        /// racine du projet Flare, dossier Content ou sous-dossier de Content.
        /// </param>
        /// <param name="templateId">
        /// ID du template détecté dans le XML et sélectionné par l'utilisateur.
        /// </param>
        public FrenchIhmVariableSetGenerationResult Generate(
            string sourceXmlPath,
            string selectedPath,
            string templateId)
        {
            ValidateArguments(sourceXmlPath, selectedPath, templateId);

            XDocument sourceDocument = XDocument.Load(
                sourceXmlPath,
                LoadOptions.PreserveWhitespace);

            List<XElement> topics = sourceDocument
                .Descendants()
                .Where(element =>
                    string.Equals(
                        element.Name.LocalName,
                        "Topic",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            XElement templateTopic = topics.FirstOrDefault(topic =>
                IsSelectedFrenchTemplate(topic, templateId));

            if (templateTopic == null)
            {
                throw new InvalidOperationException(
                    "Aucun template Topic français n'a été trouvé pour l'ID "
                    + templateId
                    + ".");
            }

            XElement templateObject = GetDirectChild(
                templateTopic,
                "Object");

            string templateDescription = GetDirectChildValue(
                templateObject,
                "Description");

            if (string.IsNullOrWhiteSpace(templateDescription))
            {
                templateDescription = "IHM_" + templateId;
            }

            string variableSetName = MakeSafeFileName(
                templateDescription);

            FrenchIhmVariableSetGenerationResult result =
                new FrenchIhmVariableSetGenerationResult
                {
                    TemplateId = templateId,
                    TemplateDescription = templateDescription,
                    VariableSetName = variableSetName
                };

            HashSet<string> usedVariableNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (XElement topic in topics)
            {
                XElement objectElement = GetDirectChild(
                    topic,
                    "Object");

                if (!IsFrenchVariableTopic(
                    objectElement,
                    templateId))
                {
                    continue;
                }

                string topicId = GetDirectChildValue(
                    objectElement,
                    "ID");

                string description = GetDirectChildValue(
                    objectElement,
                    "Description");

                XElement textElement = GetDirectChild(
                    topic,
                    "Text");

                string definition = ExtractDefinition(
                    textElement);

                if (string.IsNullOrWhiteSpace(topicId))
                {
                    result.Warnings.Add(
                        "Un topic IHM français a été ignoré car son ID est vide.");

                    continue;
                }

                string variableName = description == null
                    ? string.Empty
                    : description.Trim();

                if (string.IsNullOrWhiteSpace(variableName))
                {
                    variableName = "IHM_" + topicId;

                    result.Warnings.Add(
                        "Le topic "
                        + topicId
                        + " n'a pas de Description. "
                        + "Le nom de variable utilisé est "
                        + variableName
                        + ".");
                }

                variableName = EnsureUniqueVariableName(
                    variableName,
                    topicId,
                    usedVariableNames,
                    result.Warnings);

                if (string.IsNullOrWhiteSpace(definition))
                {
                    result.Warnings.Add(
                        "Le topic "
                        + topicId
                        + " / "
                        + variableName
                        + " possède un élément Text vide.");
                }

                FrenchIhmVariableDefinition variable =
                    new FrenchIhmVariableDefinition
                    {
                        TopicId = topicId,
                        Name = variableName,
                        Definition = definition
                    };

                result.Variables.Add(variable);

                result.TopicIdToVariableName[topicId] =
                    variableName;
            }

            result.Variables = result.Variables
                .OrderBy(
                    variable => variable.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    variable => variable.TopicId,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            string flareProjectRootPath =
                ResolveFlareProjectRoot(selectedPath);

            string variableSetFolder = Path.Combine(
                flareProjectRootPath,
                "Project",
                "VariableSets");

            Directory.CreateDirectory(
                variableSetFolder);

            string outputPath = Path.Combine(
                variableSetFolder,
                variableSetName + ".flvar");

            CreateBackupIfNeeded(
                outputPath);

            WriteVariableSet(
                outputPath,
                result.Variables);

            result.FlareProjectRootPath =
                flareProjectRootPath;

            result.OutputPath =
                outputPath;

            result.VariablesGenerated =
                result.Variables.Count;

            return result;
        }

        private void ValidateArguments(
            string sourceXmlPath,
            string selectedPath,
            string templateId)
        {
            if (string.IsNullOrWhiteSpace(sourceXmlPath))
            {
                throw new ArgumentException(
                    "Le chemin du XML Author-it est vide.");
            }

            if (!File.Exists(sourceXmlPath))
            {
                throw new FileNotFoundException(
                    "Le fichier XML Author-it est introuvable.",
                    sourceXmlPath);
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                throw new ArgumentException(
                    "Le chemin du projet ou du dossier Flare est vide.");
            }

            if (!Directory.Exists(selectedPath))
            {
                throw new DirectoryNotFoundException(
                    "Le dossier sélectionné est introuvable : "
                    + selectedPath);
            }

            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException(
                    "L'ID du template Author-it est vide.");
            }
        }

        private bool IsSelectedFrenchTemplate(
            XElement topic,
            string templateId)
        {
            XElement objectElement = GetDirectChild(
                topic,
                "Object");

            if (objectElement == null)
            {
                return false;
            }

            return string.Equals(
                       GetDirectChildValue(objectElement, "ID"),
                       templateId,
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       GetDirectChildValue(objectElement, "Type"),
                       "Topic",
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       GetDirectChildValue(objectElement, "IsTemplate"),
                       "true",
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       GetDirectChildValue(objectElement, "LocID"),
                       FrenchLocId,
                       StringComparison.OrdinalIgnoreCase);
        }

        private bool IsFrenchVariableTopic(
            XElement objectElement,
            string templateId)
        {
            if (objectElement == null)
            {
                return false;
            }

            return string.Equals(
                       GetDirectChildValue(objectElement, "Type"),
                       "Topic",
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       GetDirectChildValue(objectElement, "IsTemplate"),
                       "false",
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       GetDirectChildValue(objectElement, "LocID"),
                       FrenchLocId,
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       GetDirectChildValue(objectElement, "BasedOn"),
                       templateId,
                       StringComparison.OrdinalIgnoreCase);
        }

        private string ExtractDefinition(
            XElement textElement)
        {
            if (textElement == null)
            {
                return string.Empty;
            }

            StringBuilder builder =
                new StringBuilder();

            foreach (XNode node in textElement.Nodes())
            {
                AppendPlainText(
                    node,
                    builder);
            }

            string text = builder
                .ToString()
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");

            text = Regex
                .Replace(text, " {2,}", " ")
                .Trim();

            return text.Replace(
                NonBreakingSpaceMarker,
                "\u00A0");
        }

        private void AppendPlainText(
            XNode node,
            StringBuilder builder)
        {
            XText textNode = node as XText;

            if (textNode != null)
            {
                builder.Append(
                    textNode.Value);

                return;
            }

            XElement element = node as XElement;

            if (element == null)
            {
                return;
            }

            string localName =
                element.Name.LocalName;

            if (string.Equals(
                localName,
                "nbs",
                StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(
                    NonBreakingSpaceMarker);

                return;
            }

            if (string.Equals(
                localName,
                "br",
                StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(" ");
                return;
            }

            foreach (XNode childNode in element.Nodes())
            {
                AppendPlainText(
                    childNode,
                    builder);
            }

            if (string.Equals(
                localName,
                "p",
                StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(" ");
            }
        }

        private string EnsureUniqueVariableName(
            string requestedName,
            string topicId,
            HashSet<string> usedNames,
            List<string> warnings)
        {
            string finalName =
                requestedName.Trim();

            if (usedNames.Add(finalName))
            {
                return finalName;
            }

            string baseName =
                finalName + "_" + topicId;

            finalName = baseName;

            int suffix = 2;

            while (!usedNames.Add(finalName))
            {
                finalName =
                    baseName + "_" + suffix;

                suffix++;
            }

            warnings.Add(
                "Le nom de variable \""
                + requestedName
                + "\" est présent plusieurs fois. "
                + "Le topic "
                + topicId
                + " a été renommé \""
                + finalName
                + "\".");

            return finalName;
        }

        private void WriteVariableSet(
            string outputPath,
            List<FrenchIhmVariableDefinition> variables)
        {
            XElement root =
                new XElement(
                    "CatapultVariableSet");

            foreach (FrenchIhmVariableDefinition variable in variables)
            {
                string name =
                    variable.Name ?? string.Empty;

                string topicId =
                    variable.TopicId ?? string.Empty;

                string definition =
                    variable.Definition ?? string.Empty;

                XElement variableElement =
                    new XElement(
                        "Variable",
                        new XAttribute("Name", name),
                        new XAttribute("Comment", topicId),
                        new XAttribute("EvaluatedDefinition", definition),
                        definition);

                root.Add(
                    variableElement);
            }

            XDocument outputDocument =
                new XDocument(
                    new XDeclaration(
                        "1.0",
                        "utf-8",
                        null),
                    root);

            XmlWriterSettings settings =
                new XmlWriterSettings
                {
                    Encoding =
                        new UTF8Encoding(false),
                    Indent = true,
                    OmitXmlDeclaration = false,
                    NewLineChars =
                        Environment.NewLine,
                    NewLineHandling =
                        NewLineHandling.Replace
                };

            using (XmlWriter writer =
                XmlWriter.Create(
                    outputPath,
                    settings))
            {
                outputDocument.Save(
                    writer);
            }
        }

        private string ResolveFlareProjectRoot(
            string selectedPath)
        {
            DirectoryInfo current =
                new DirectoryInfo(selectedPath);

            while (current != null)
            {
                string projectFolder = Path.Combine(
                    current.FullName,
                    "Project");

                string contentFolder = Path.Combine(
                    current.FullName,
                    "Content");

                if (Directory.Exists(projectFolder)
                    && Directory.Exists(contentFolder))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                "Impossible de retrouver la racine du projet Flare depuis : "
                + selectedPath
                + Environment.NewLine
                + "La racine du projet doit contenir les dossiers Project et Content.");
        }

        private void CreateBackupIfNeeded(
            string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                return;
            }

            string backupPath =
                outputPath
                + "."
                + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                + ".bak";

            File.Copy(
                outputPath,
                backupPath,
                true);
        }

        private XElement GetDirectChild(
            XElement parent,
            string localName)
        {
            if (parent == null)
            {
                return null;
            }

            return parent
                .Elements()
                .FirstOrDefault(element =>
                    string.Equals(
                        element.Name.LocalName,
                        localName,
                        StringComparison.OrdinalIgnoreCase));
        }

        private string GetDirectChildValue(
            XElement parent,
            string localName)
        {
            XElement child = GetDirectChild(
                parent,
                localName);

            return child == null
                ? string.Empty
                : child.Value.Trim();
        }

        private string MakeSafeFileName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "IHM_Variables";
            }

            string result =
                value.Trim();

            foreach (char invalidCharacter
                in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(
                    invalidCharacter,
                    '_');
            }

            return result;
        }
    }

    public class FrenchIhmVariableSetGenerationResult
    {
        public FrenchIhmVariableSetGenerationResult()
        {
            Variables =
                new List<FrenchIhmVariableDefinition>();

            Warnings =
                new List<string>();

            TopicIdToVariableName =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
        }

        public string TemplateId { get; set; }

        public string TemplateDescription { get; set; }

        public string VariableSetName { get; set; }

        public string FlareProjectRootPath { get; set; }

        public string OutputPath { get; set; }

        public int VariablesGenerated { get; set; }

        public List<FrenchIhmVariableDefinition> Variables { get; set; }

        public List<string> Warnings { get; set; }

        public Dictionary<string, string> TopicIdToVariableName { get; set; }
    }

    public class FrenchIhmVariableDefinition
    {
        public string TopicId { get; set; }

        public string Name { get; set; }

        public string Definition { get; set; }
    }
}