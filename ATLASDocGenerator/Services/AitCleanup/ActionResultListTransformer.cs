using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class ActionResultListTransformer
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

                    int actionNumDetectedInFile = 0;
                    int actionBulletDetectedInFile = 0;
                    int resultDetectedInFile = 0;
                    int listsCreatedInFile = 0;

                    bool changed = false;

                    List<XElement> containers = document
                        .Descendants()
                        .Where(HasDirectActionOrResultChild)
                        .ToList();

                    foreach (XElement container in containers)
                    {
                        bool containerChanged = TransformContainer(
                            container,
                            ref actionNumDetectedInFile,
                            ref actionBulletDetectedInFile,
                            ref resultDetectedInFile,
                            ref listsCreatedInFile
                        );

                        if (containerChanged)
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        document.Save(filePath, SaveOptions.DisableFormatting);

                        report.ActionResultListsTransformed += listsCreatedInFile;

                        string detail =
                            Path.GetFileName(filePath)
                            + " | transformed lists: " + listsCreatedInFile
                            + " | a_action_num: " + actionNumDetectedInFile
                            + " | a_action/a_action_b: " + actionBulletDetectedInFile
                            + " | a_resultat/a_resultat_b: " + resultDetectedInFile;

                        report.ActionResultDetectionDetails.Add(detail);
                    }

                    report.ActionNumParagraphsDetected += actionNumDetectedInFile;
                    report.ActionBulletParagraphsDetected += actionBulletDetectedInFile;
                    report.ResultParagraphsDetected += resultDetectedInFile;
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Action/result transformation failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        private bool TransformContainer(
            XElement container,
            ref int actionNumDetected,
            ref int actionBulletDetected,
            ref int resultDetected,
            ref int listsCreated)
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

                string actionKind;

                if (TryGetActionKind(current, out actionKind))
                {
                    XNamespace ns = current.Name.Namespace;

                    XElement listElement;

                    if (actionKind == "numbered")
                    {
                        listElement = new XElement(ns + "ol");
                        listElement.SetAttributeValue("class", "Action_num");
                    }
                    else
                    {
                        listElement = new XElement(ns + "ul");
                        listElement.SetAttributeValue("class", "Action_bullet");
                    }

                    while (index < children.Count)
                    {
                        XElement actionElement = children[index];
                        string nextActionKind;

                        if (!TryGetActionKind(actionElement, out nextActionKind) || nextActionKind != actionKind)
                        {
                            break;
                        }

                        if (actionKind == "numbered")
                        {
                            actionNumDetected++;
                        }
                        else
                        {
                            actionBulletDetected++;
                        }

                        XElement listItem = new XElement(ns + "li");

                        XElement actionParagraph = CloneParagraphWithoutClass(actionElement);
                        listItem.Add(actionParagraph);

                        index++;

                        XElement resultList = null;

                        while (index < children.Count)
                        {
                            XElement nextElement = children[index];

                            if (IsIgnorableEmptyParagraph(nextElement))
                            {
                                index++;
                                continue;
                            }

                            if (IsResultParagraph(nextElement))
                            {
                                if (resultList == null)
                                {
                                    resultList = new XElement(ns + "ul");
                                    listItem.Add(resultList);
                                }

                                XElement resultItem = new XElement(ns + "li");
                                resultItem.Add(CloneParagraphWithoutClass(nextElement));
                                resultList.Add(resultItem);

                                resultDetected++;
                                index++;
                                continue;
                            }

                            if (IsCenteredImageParagraph(nextElement))
                            {
                                XElement imageParagraph = CloneParagraphWithClass(nextElement, "a_centre");
                                listItem.Add(imageParagraph);

                                index++;
                                continue;
                            }

                            break;
                        }

                        listElement.Add(listItem);
                    }

                    newNodes.Add(listElement);
                    listsCreated++;
                    changed = true;
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

        private bool HasDirectActionOrResultChild(XElement element)
        {
            return element
                .Elements()
                .Any(child =>
                    TryGetActionKind(child, out _) ||
                    IsResultParagraph(child)
                );
        }

        private bool TryGetActionKind(XElement element, out string actionKind)
        {
            actionKind = null;

            if (!IsParagraph(element))
            {
                return false;
            }

            if (HasClass(element, "a_action_num"))
            {
                actionKind = "numbered";
                return true;
            }

            if (HasClass(element, "a_action") || HasClass(element, "a_action_b"))
            {
                actionKind = "bullet";
                return true;
            }

            return false;
        }

        private bool IsResultParagraph(XElement element)
        {
            return IsParagraph(element)
                && (HasClass(element, "a_resultat") || HasClass(element, "a_resultat_b"));
        }

        private bool IsCenteredImageParagraph(XElement element)
        {
            if (!IsParagraph(element))
            {
                return false;
            }

            bool hasCenteredClass =
                HasClass(element, "a_normal_centered") ||
                HasClass(element, "A_NORMAL_centered");

            if (!hasCenteredClass)
            {
                return false;
            }

            return element
                .Descendants()
                .Any(descendant => descendant.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        private XElement CloneParagraphWithoutClass(XElement paragraph)
        {
            XElement clone = new XElement(paragraph);
            clone.Attribute("class")?.Remove();
            return clone;
        }

        private XElement CloneParagraphWithClass(XElement paragraph, string className)
        {
            XElement clone = new XElement(paragraph);
            clone.SetAttributeValue("class", className);
            return clone;
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
                .Replace("&#160;", "")
                .Trim();

            if (!string.IsNullOrEmpty(text))
            {
                return false;
            }

            return true;
        }
    }
}
