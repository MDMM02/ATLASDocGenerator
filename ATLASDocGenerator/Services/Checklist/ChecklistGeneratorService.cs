using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using B3.PluginAPIKit;

namespace ATLASDocGenerator.Services.Checklist
{
    public class ChecklistGeneratorService
    {
        private const string ChecklistClass = "atlas-checklist";

        public string LastGeneratedFilePath { get; private set; }

        public int GenerateChecklistFromActiveDocument(IDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            string sourceUrl = document.GetSourceUrl();
            string filePath = ResolveFilePath(sourceUrl);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The active topic file could not be found.", filePath);

            LastGeneratedFilePath = filePath;

            string xmlText = File.ReadAllText(filePath, Encoding.UTF8);

            XDocument xdoc = XDocument.Parse(xmlText, LoadOptions.PreserveWhitespace);

            XElement root = xdoc.Root;

            if (root == null)
                throw new InvalidOperationException("The topic XML has no root element.");

            XElement body = root.Elements().FirstOrDefault(e => e.Name.LocalName == "body");

            if (body == null)
            {
                body = new XElement("body");

                XElement head = root.Elements().FirstOrDefault(e => e.Name.LocalName == "head");

                if (head != null)
                {
                    head.AddAfterSelf(body);
                }
                else
                {
                    root.AddFirst(body);
                }

                List<XElement> misplacedContent = root.Elements()
                    .Where(e => e.Name.LocalName != "head" && e.Name.LocalName != "body")
                    .ToList();

                foreach (XElement element in misplacedContent)
                {
                    element.Remove();
                    body.Add(element);
                }
            }

            List<string> h1Titles = ExtractH1Titles(body);

            if (h1Titles.Count == 0)
                throw new InvalidOperationException("No H1 titles found in the active topic.");

            XElement existingChecklist = body.Elements()
                .FirstOrDefault(e => HasClass(e, ChecklistClass));

            XElement checklist = BuildChecklistElement(h1Titles);

            if (existingChecklist != null)
            {
                existingChecklist.ReplaceWith(checklist);
            }
            else
            {
                body.Add(new XText(Environment.NewLine));
                body.Add(checklist);
                body.Add(new XText(Environment.NewLine));
            }

            string backupPath = filePath + ".atlas-checklist.bak";
            File.Copy(filePath, backupPath, true);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                xdoc.Save(writer);
            }

            return h1Titles.Count;
        }

        private string ResolveFilePath(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new InvalidOperationException("The active document has no source URL.");

            Uri uri;

            if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out uri) && uri.IsFile)
                return uri.LocalPath;

            return sourceUrl;
        }

        private List<string> ExtractH1Titles(XElement body)
        {
            return body
                .Descendants()
                .Where(e => e.Name.LocalName == "h1")
                .Where(e => !IsInsideGeneratedChecklist(e))
                .Select(e => NormalizeText(e.Value))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => !t.Equals("Checklist", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private bool IsInsideGeneratedChecklist(XElement element)
        {
            return element
                .Ancestors()
                .Any(a => HasClass(a, ChecklistClass));
        }

        private bool HasClass(XElement element, string className)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
                return false;

            string[] classes = classAttribute.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return classes.Contains(className);
        }

        private string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(
                " ",
                value.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            );
        }

        private XElement BuildChecklistElement(List<string> h1Titles)
        {
            XElement wrapper = new XElement(
                "div",
                new XAttribute("class", ChecklistClass),
                new XElement(
                    "p",
                    new XAttribute("class", "atlas-checklist-title"),
                    new XAttribute("style", "font-weight: bold; font-size: 16pt; margin-top: 24px;"),
                    "Checklist"
                ),
                new XElement(
                    "p",
                    "Compléter la checklist ci-dessous avant de clôturer l’intervention. Si NOK ou N/A est sélectionné, ajouter un commentaire."
                )
            );

            foreach (string title in h1Titles)
            {
                XElement item = new XElement(
                    "div",
                    new XAttribute("class", "atlas-checklist-item"),
                    new XAttribute("style", "border: 1px solid #000; padding: 8px; margin-bottom: 10px; page-break-inside: avoid;"),
                    new XElement(
                        "p",
                        new XAttribute("style", "font-weight: bold; margin-bottom: 6px;"),
                        title
                    ),
                    new XElement(
                        "p",
                        new XAttribute("style", "margin-bottom: 6px;"),
                        "[ ] OK     [ ] NOK     [ ] N/A"
                    ),
                    new XElement(
                        "p",
                        new XAttribute("style", "margin-bottom: 2px;"),
                        "Commentaires :"
                    ),
                    new XElement(
                        "p",
                        new XAttribute("style", "margin: 0 0 4px 0;"),
                        "____________________________________________________________________________________________________________________"
                    ),
                    new XElement(
                        "p",
                        new XAttribute("style", "margin: 0 0 4px 0;"),
                        "____________________________________________________________________________________________________________________"
                    ),
                    new XElement(
                        "p",
                        new XAttribute("style", "margin: 0 0 4px 0;"),
                        "____________________________________________________________________________________________________________________"
                    )
                );

                wrapper.Add(item);
            }

            return wrapper;
        }

    }
}