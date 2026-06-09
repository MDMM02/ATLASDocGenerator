using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class CalloutTransformer
    {
        public void Transform(IEnumerable<string> htmlFiles, CleanupReport report)
        {
            if (htmlFiles == null)
            {
                throw new ArgumentNullException("htmlFiles");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            foreach (string filePath in htmlFiles)
            {
                try
                {
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int calloutsInFile = 0;
                    bool changed = false;

                    List<XElement> containers = document
                        .Descendants()
                        .Where(HasDirectCalloutCandidate)
                        .ToList();

                    foreach (XElement container in containers)
                    {
                        bool containerChanged = TransformContainer(container, filePath, report, ref calloutsInFile);

                        if (containerChanged)
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        document.Save(filePath, SaveOptions.DisableFormatting);

                        report.CalloutsTransformed += calloutsInFile;

                        report.CalloutTransformationDetails.Add(
                            Path.GetFileName(filePath) + " | callouts transformed: " + calloutsInFile
                        );
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Callout transformation failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        private bool TransformContainer(XElement container, string filePath, CleanupReport report, ref int calloutsInFile)
        {
            List<XElement> children = container.Elements().ToList();

            if (children.Count == 0)
            {
                return false;
            }

            bool changed = false;
            List<XNode> newNodes = new List<XNode>();

            int index = 0;

            while (index < children.Count)
            {
                XElement current = children[index];

                if (IsCalloutSpacingParagraph(current) && HasNextCalloutTable(children, index))
                {
                    index++;
                    changed = true;
                    continue;
                }

                XElement calloutDiv;

                if (TryBuildCalloutDiv(current, filePath, report, out calloutDiv))
                {
                    newNodes.Add(calloutDiv);
                    calloutsInFile++;
                    changed = true;
                    index++;
                    continue;
                }

                newNodes.Add(new XElement(current));
                index++;
            }

            if (changed)
            {
                container.ReplaceNodes(newNodes);
            }

            return changed;
        }

        private bool TryBuildCalloutDiv(XElement table, string filePath, CleanupReport report, out XElement calloutDiv)
        {
            calloutDiv = null;

            if (!IsTable(table))
            {
                return false;
            }

            XElement firstRow = table
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName.Equals("tr", StringComparison.OrdinalIgnoreCase));

            if (firstRow == null)
            {
                return false;
            }

            List<XElement> cells = firstRow
                .Elements()
                .Where(element =>
                    element.Name.LocalName.Equals("td", StringComparison.OrdinalIgnoreCase)
                    || element.Name.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cells.Count < 2)
            {
                return false;
            }

            XElement iconCell = cells[0];
            XElement contentCell = cells[1];

            XElement iconImage = iconCell
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));

            if (iconImage == null)
            {
                return false;
            }

            string iconSource = GetAttributeValue(iconImage, "src");
            bool usedFallback;
            string calloutClass = ResolveCalloutClass(iconSource, table, out usedFallback);

            XNamespace ns = table.Name.Namespace;

            XElement div = new XElement(ns + "div");
            div.SetAttributeValue("class", calloutClass);

            int contentCount = 0;

            foreach (XElement contentElement in contentCell.Elements())
            {
                if (IsIgnorableEmptyParagraph(contentElement))
                {
                    continue;
                }

                XElement clone = new XElement(contentElement);
                CleanCalloutContentElement(clone);

                div.Add(clone);
                contentCount++;
            }

            if (contentCount == 0)
            {
                report.Warnings.Add("Callout table ignored because no content was found: " + filePath);
                return false;
            }

            if (usedFallback)
            {
                report.Warnings.Add(
                    "Callout icon not recognized, defaulted to a_Attention: "
                    + Path.GetFileName(filePath)
                    + " | icon src: "
                    + iconSource
                );
            }

            calloutDiv = div;
            return true;
        }

        private string ResolveCalloutClass(string iconSource, XElement table, out bool usedFallback)
        {
            usedFallback = false;

            string tableStyle = GetAttributeValue(table, "style");

            // INFORMATION = bleu
            // Exemple observé : Image5150.jpg avec pictogramme bleu.
            if (ContainsIgnoreCase(iconSource, "information")
                || ContainsIgnoreCase(iconSource, "info")
                || ContainsIgnoreCase(iconSource, "Image5150")
                || ContainsIgnoreCase(tableStyle, "#0070C0")
                || ContainsIgnoreCase(tableStyle, "#005EB8")
                || ContainsIgnoreCase(tableStyle, "#4472C4")
                || ContainsIgnoreCase(tableStyle, "blue"))
            {
                return "a_Information";
            }

            // PRECAUTION = orange
            if (ContainsIgnoreCase(iconSource, "precaution")
                || ContainsIgnoreCase(iconSource, "warning")
                || ContainsIgnoreCase(iconSource, "caution")
                || ContainsIgnoreCase(tableStyle, "#B57406")
                || ContainsIgnoreCase(tableStyle, "#F4B183")
                || ContainsIgnoreCase(tableStyle, "orange"))
            {
                return "a_Precaution";
            }

            // ATTENTION = rouge
            if (ContainsIgnoreCase(iconSource, "attention")
                || ContainsIgnoreCase(iconSource, "danger")
                || ContainsIgnoreCase(iconSource, "alert")
                || ContainsIgnoreCase(tableStyle, "#C00000")
                || ContainsIgnoreCase(tableStyle, "#FF0000")
                || ContainsIgnoreCase(tableStyle, "red"))
            {
                return "a_Attention";
            }

            usedFallback = true;
            return "a_Information";
        }

        private void CleanCalloutContentElement(XElement element)
        {
            if (IsParagraph(element) && !ShouldPreserveParagraphClass(element))
            {
                XAttribute classAttribute = element.Attribute("class");

                if (classAttribute != null)
                {
                    classAttribute.Remove();
                }
            }

            XAttribute styleAttribute = element.Attribute("style");

            if (styleAttribute != null)
            {
                styleAttribute.Remove();
            }

            XAttribute widthAttribute = element.Attribute("width");

            if (widthAttribute != null)
            {
                widthAttribute.Remove();
            }

            foreach (XElement descendant in element.Descendants().ToList())
            {
                if (IsParagraph(descendant) && !ShouldPreserveParagraphClass(descendant))
                {
                    XAttribute descendantClassAttribute = descendant.Attribute("class");

                    if (descendantClassAttribute != null)
                    {
                        descendantClassAttribute.Remove();
                    }
                }

                XAttribute descendantStyleAttribute = descendant.Attribute("style");

                if (descendantStyleAttribute != null)
                {
                    descendantStyleAttribute.Remove();
                }

                XAttribute descendantWidthAttribute = descendant.Attribute("width");

                if (descendantWidthAttribute != null)
                {
                    descendantWidthAttribute.Remove();
                }
            }
        }

        private bool ShouldPreserveParagraphClass(XElement element)
        {
            return IsBulletParagraph(element)
                || HasClass(element, "a_action")
                || HasClass(element, "a_action_b")
                || HasClass(element, "a_action_num")
                || HasClass(element, "a_resultat")
                || HasClass(element, "a_resultat_b")
                || HasClass(element, "a_normal_centered")
                || HasClass(element, "A_NORMAL_centered");
        }

        private bool HasDirectCalloutCandidate(XElement element)
        {
            return element
                .Elements()
                .Any(child => IsCalloutSpacingParagraph(child) || IsTable(child));
        }

        private bool HasNextCalloutTable(List<XElement> children, int currentIndex)
        {
            for (int i = currentIndex + 1; i < children.Count; i++)
            {
                if (IsCalloutSpacingParagraph(children[i]) || IsIgnorableEmptyParagraph(children[i]))
                {
                    continue;
                }

                return IsPotentialCalloutTable(children[i]);
            }

            return false;
        }

        private bool IsPotentialCalloutTable(XElement element)
        {
            if (!IsTable(element))
            {
                return false;
            }

            return element
                .Descendants()
                .Any(descendant => descendant.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTable(XElement element)
        {
            return element.Name.LocalName.Equals("table", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCalloutSpacingParagraph(XElement element)
        {
            return IsParagraph(element)
                && HasClass(element, "a_mqt_videencradre");
        }

        private bool IsIgnorableEmptyParagraph(XElement element)
        {
            if (!IsParagraph(element))
            {
                return false;
            }

            bool hasImage = element
                .Descendants()
                .Any(descendant => descendant.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));

            if (hasImage)
            {
                return false;
            }

            string text = element.Value
                .Replace("\u00A0", "")
                .Trim();

            return string.IsNullOrEmpty(text);
        }

        private bool IsBulletParagraph(XElement element)
        {
            if (!IsParagraph(element))
            {
                return false;
            }

            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
            {
                return false;
            }

            string[] classes = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return classes.Any(c => c.StartsWith("a_tiret", StringComparison.OrdinalIgnoreCase));
        }

        private bool HasClass(XElement element, string className)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
            {
                return false;
            }

            string[] classes = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return classes.Any(c => c.Equals(className, StringComparison.OrdinalIgnoreCase));
        }

        private string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return string.Empty;
            }

            return attribute.Value;
        }

        private bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}