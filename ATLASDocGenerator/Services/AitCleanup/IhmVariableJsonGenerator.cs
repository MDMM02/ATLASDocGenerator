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

            XDocument document = XDocument.Load(sourceXmlPath);

            string ihmStyleId = FindIhmStyleId(document);

            if (string.IsNullOrWhiteSpace(ihmStyleId))
                throw new Exception("Style A_ihm was not found in the Author-it XML.");

            Dictionary<string, int> ihmTexts = ExtractIhmTexts(document, ihmStyleId);

            string atlasFolder = Path.Combine(flareProjectRootPath, "Project", "ATLAS");
            Directory.CreateDirectory(atlasFolder);

            string outputPath = Path.Combine(atlasFolder, "IhmVariables.json");

            IhmVariableJsonModel model = new IhmVariableJsonModel
            {
                SourceXmlPath = sourceXmlPath,
                StyleName = "A_ihm",
                StyleId = ihmStyleId,
                GeneratedAt = DateTime.Now.ToString("s"),
                Items = ihmTexts
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
            XElement style = document
                .Descendants()
                .FirstOrDefault(e =>
                    string.Equals(e.Name.LocalName, "Style", StringComparison.OrdinalIgnoreCase)
                    && e.Descendants().Any(d =>
                        (string.Equals(d.Name.LocalName, "Description", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(d.Name.LocalName, "StyleName", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(d.Name.LocalName, "PrintStyleName", StringComparison.OrdinalIgnoreCase))
                        && string.Equals((d.Value ?? "").Trim(), "A_ihm", StringComparison.OrdinalIgnoreCase)));

            if (style == null)
                return null;

            XElement idElement = style
                .Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "ID", StringComparison.OrdinalIgnoreCase));

            return idElement == null ? null : idElement.Value.Trim();
        }

        private Dictionary<string, int> ExtractIhmTexts(XDocument document, string ihmStyleId)
        {
            Dictionary<string, int> results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<XElement> ihmElements = document
                .Descendants()
                .Where(e =>
                    string.Equals(e.Name.LocalName, "cs", StringComparison.OrdinalIgnoreCase)
                    && string.Equals((string)e.Attribute("id"), ihmStyleId, StringComparison.OrdinalIgnoreCase));

            foreach (XElement element in ihmElements)
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

            foreach (char ch in json)
            {
                if (ch == '"')
                {
                    pretty.Append(ch);
                    quoted = !quoted;
                }
                else if (!quoted && (ch == '{' || ch == '['))
                {
                    pretty.Append(ch);
                    pretty.AppendLine();
                    pretty.Append(new string(' ', ++indent * 2));
                }
                else if (!quoted && (ch == '}' || ch == ']'))
                {
                    pretty.AppendLine();
                    pretty.Append(new string(' ', --indent * 2));
                    pretty.Append(ch);
                }
                else if (!quoted && ch == ',')
                {
                    pretty.Append(ch);
                    pretty.AppendLine();
                    pretty.Append(new string(' ', indent * 2));
                }
                else if (!quoted && ch == ':')
                {
                    pretty.Append(": ");
                }
                else
                {
                    pretty.Append(ch);
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