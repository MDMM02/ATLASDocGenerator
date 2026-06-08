using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using B3.PluginAPIKit;

namespace ATLASDocGenerator.Services.Checklist
{
    public class ChecklistGeneratorService
    {
        private const string StartMarker = "ATLAS_CHECKLIST_START";
        private const string EndMarker = "ATLAS_CHECKLIST_END";

        public int GenerateChecklistFromActiveDocument(IDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            XmlDocument xmlDoc = document.GetXmlDocument();

            XmlNode bodyNode = xmlDoc.SelectSingleNode("//body");

            if (bodyNode == null)
                throw new InvalidOperationException("No body element found in the active topic.");

            List<string> h1Titles = ExtractH1Titles(xmlDoc);

            if (h1Titles.Count == 0)
                throw new InvalidOperationException("No H1 titles found in the active topic.");

            XmlNode existingChecklist = FindExistingChecklist(bodyNode);
            XmlNode checklistNode = BuildChecklistNode(xmlDoc, h1Titles);

            document.StartOperation("Generate ATLAS Checklist");

            try
            {
                if (existingChecklist != null)
                    document.RemoveDocumentNode(existingChecklist, true);

                document.InsertDocumentNode(
                    checklistNode,
                    bodyNode,
                    bodyNode.ChildNodes.Count,
                    true
                );

                document.EndOperation();
                document.UpdateView();
                document.Save();
            }
            catch
            {
                document.EndOperation();
                throw;
            }

            return h1Titles.Count;
        }

        private List<string> ExtractH1Titles(XmlDocument xmlDoc)
        {
            XmlNodeList h1Nodes = xmlDoc.SelectNodes("//h1");

            List<string> titles = new List<string>();

            foreach (XmlNode h1 in h1Nodes)
            {
                string title = h1.InnerText.Trim();

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                if (title.Equals("Checklist", StringComparison.OrdinalIgnoreCase))
                    continue;

                titles.Add(title);
            }

            return titles;
        }

        private XmlNode FindExistingChecklist(XmlNode bodyNode)
        {
            foreach (XmlNode child in bodyNode.ChildNodes)
            {
                if (child.Name != "div")
                    continue;

                bool hasStartMarker = child.ChildNodes
                    .Cast<XmlNode>()
                    .Any(node =>
                        node.NodeType == XmlNodeType.Comment &&
                        node.Value != null &&
                        node.Value.Contains(StartMarker)
                    );

                if (hasStartMarker)
                    return child;
            }

            return null;
        }

        private XmlNode BuildChecklistNode(XmlDocument xmlDoc, List<string> h1Titles)
        {
            XmlElement wrapper = xmlDoc.CreateElement("div");
            wrapper.SetAttribute("class", "atlas-checklist");

            wrapper.AppendChild(xmlDoc.CreateComment(StartMarker));

            XmlElement title = xmlDoc.CreateElement("h1");
            title.InnerText = "Checklist";
            wrapper.AppendChild(title);

            XmlElement intro = xmlDoc.CreateElement("p");
            intro.InnerText = "Complete the checklist below before closing the intervention. If NOK or N/A is selected, add a comment.";
            wrapper.AppendChild(intro);

            XmlElement table = xmlDoc.CreateElement("table");
            table.SetAttribute("class", "atlas-checklist-table");
            table.SetAttribute("style", "width: 100%; border-collapse: collapse;");

            XmlElement thead = xmlDoc.CreateElement("thead");
            XmlElement headerRow = xmlDoc.CreateElement("tr");

            AddCell(xmlDoc, headerRow, "th", "Section");
            AddCell(xmlDoc, headerRow, "th", "OK");
            AddCell(xmlDoc, headerRow, "th", "NOK");
            AddCell(xmlDoc, headerRow, "th", "N/A");
            AddCell(xmlDoc, headerRow, "th", "Commentaires");

            thead.AppendChild(headerRow);
            table.AppendChild(thead);

            XmlElement tbody = xmlDoc.CreateElement("tbody");

            foreach (string h1Title in h1Titles)
            {
                XmlElement row = xmlDoc.CreateElement("tr");

                AddCell(xmlDoc, row, "td", h1Title);
                AddCell(xmlDoc, row, "td", "☐");
                AddCell(xmlDoc, row, "td", "☐");
                AddCell(xmlDoc, row, "td", "☐");
                AddCell(xmlDoc, row, "td", "");

                tbody.AppendChild(row);
            }

            table.AppendChild(tbody);
            wrapper.AppendChild(table);

            wrapper.AppendChild(xmlDoc.CreateComment(EndMarker));

            return wrapper;
        }

        private void AddCell(XmlDocument xmlDoc, XmlElement row, string cellType, string text)
        {
            XmlElement cell = xmlDoc.CreateElement(cellType);
            cell.InnerText = text;
            cell.SetAttribute("style", "border: 1px solid #000; padding: 4px;");
            row.AppendChild(cell);
        }
    }
}