using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class IhmVariableJsonGenerator
    {
        public string Generate(string sourceXmlPath, string flareProjectRootPath)
        {
            if (string.IsNullOrWhiteSpace(sourceXmlPath))
                throw new ArgumentException("Source XML path is empty.");

            if (!File.Exists(sourceXmlPath))
                throw new FileNotFoundException("Source XML file not found.", sourceXmlPath);

            if (string.IsNullOrWhiteSpace(flareProjectRootPath))
                throw new ArgumentException("Flare project root path is empty.");

            XDocument document = XDocument.Load(sourceXmlPath);

            string ihmStyleId = FindIhmStyleId(document);

            if (string.IsNullOrWhiteSpace(ihmStyleId))
                throw new Exception("Style A_ihm was not found in the Author-it XML.");

            Dictionary<string, int> items = ExtractIhmTexts(document, ihmStyleId);

            string atlasFolder = Path.Combine(flareProjectRootPath, "Project", "ATLAS");

            if (!Directory.Exists(atlasFolder))
                Directory.CreateDirectory(atlasFolder);

            string outputPath = Path.Combine(atlasFolder, "IhmVariables.json");

            IhmVariableJsonModel model = new IhmVariableJsonModel
            {
                SourceXmlPath = sourceXmlPath,
                StyleName = "A_ihm",
                StyleId = ihmStyleId,
                GeneratedAt = DateTime.Now.ToString("s"),
                Items = items
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new IhmVariableJsonItem
                    {
                        Text = x.Key,
                        Count = x.Value
                    })
                    .ToList()
            };

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(model);

            File.WriteAllText(outputPath, PrettyPrintJson(json), Encoding.UTF8);

            return outputPath;
        }

        private string FindIhmStyleId(XDocument document)
        {
            XElement styleElement = document
                .Descendants()
                .FirstOrDefault(e =>
                    string.Equals((string)e.Attribute("name"), "A_ihm", StringComparison.OrdinalIgnoreCase)
                    || string.Equals((string)e.Attribute("Name"), "A_ihm", StringComparison.OrdinalIgnoreCase));

            if (styleElement == null)
            {
                styleElement = document
                    .Descendants()
                    .FirstOrDefault(e =>
                        string.Equals(e.Value.Trim(), "A_ihm", StringComparison.OrdinalIgnoreCase));
            }

            if (styleElement == null)
                return null;

            XAttribute idAttribute =
                styleElement.Attribute("id") ??
                styleElement.Attribute("ID") ??
                styleElement.Attribute("Id");

            if (idAttribute != null)
                return idAttribute.Value;

            XElement parentWithId = styleElement
                .AncestorsAndSelf()
                .FirstOrDefault(e =>
                    e.Attribute("id") != null ||
                    e.Attribute("ID") != null ||
                    e.Attribute("Id") != null);

            if (parentWithId == null)
                return null;

            return ((string)parentWithId.Attribute("id"))
                ?? ((string)parentWithId.Attribute("ID"))
                ?? ((string)parentWithId.Attribute("Id"));
        }

        private Dictionary<string, int> ExtractIhmTexts(XDocument document, string ihmStyleId)
        {
            Dictionary<string, int> results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<XElement> characterStyleElements = document
                .Descendants()
                .Where(e =>
                    string.Equals(e.Name.LocalName, "cs", StringComparison.OrdinalIgnoreCase)
                    && string.Equals((string)e.Attribute("id"), ihmStyleId, StringComparison.OrdinalIgnoreCase));

            foreach (XElement element in characterStyleElements)
            {
                string text = NormalizeText(element.Value);

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (!results.ContainsKey(text))
                    results[text] = 0;

                results[text]++;
            }

            return results;
        }

        private string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
        }

        private string PrettyPrintJson(string json)
        {
            StringBuilder pretty = new StringBuilder();
            int indent = 0;
            bool quoted = false;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];

                switch (ch)
                {
                    case '"':
                        pretty.Append(ch);
                        bool escaped = false;
                        int index = i;
                        while (index > 0 && json[--index] == '\\')
                            escaped = !escaped;

                        if (!escaped)
                            quoted = !quoted;

                        break;

                    case '{':
                    case '[':
                        pretty.Append(ch);

                        if (!quoted)
                        {
                            pretty.AppendLine();
                            pretty.Append(new string(' ', ++indent * 2));
                        }

                        break;

                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            pretty.AppendLine();
                            pretty.Append(new string(' ', --indent * 2));
                        }

                        pretty.Append(ch);
                        break;

                    case ',':
                        pretty.Append(ch);

                        if (!quoted)
                        {
                            pretty.AppendLine();
                            pretty.Append(new string(' ', indent * 2));
                        }

                        break;

                    case ':':
                        pretty.Append(ch);

                        if (!quoted)
                            pretty.Append(" ");

                        break;

                    default:
                        pretty.Append(ch);
                        break;
                }
            }

            return pretty.ToString();
        }
    }

    public class IhmVariableJsonModel
    {
        public string SourceXmlPath { get; set; }
        public string StyleName { get; set; }
        public string StyleId { get; set; }
        public string GeneratedAt { get; set; }
        public List<IhmVariableJsonItem> Items { get; set; }
    }

    public class IhmVariableJsonItem
    {
        public string Text { get; set; }
        public int Count { get; set; }
    }
}