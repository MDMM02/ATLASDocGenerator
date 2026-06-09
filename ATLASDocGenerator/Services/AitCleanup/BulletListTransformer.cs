using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class BulletListTransformer
    {
        private const int NoPageBreakThreshold = 8;

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

                    int bulletParagraphsInFile = 0;
                    int bulletListsCreatedInFile = 0;
                    int noPageBreakCreatedInFile = 0;

                    bool changed = false;

                    List<XElement> containers = document
                        .Descendants()
                        .Where(HasDirectBulletChild)
                        .ToList();

                    foreach (XElement container in containers)
                    {
                        bool containerChanged = TransformContainer(
                            container,
                            ref bulletParagraphsInFile,
                            ref bulletListsCreatedInFile,
                            ref noPageBreakCreatedInFile
                        );

                        if (containerChanged)
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        document.Save(filePath, SaveOptions.DisableFormatting);

                        report.BulletParagraphsDetected += bulletParagraphsInFile;
                        report.BulletListsTransformed += bulletListsCreatedInFile;
                        report.NoPageBreakBlocksCreated += noPageBreakCreatedInFile;

                        string detail =
                            Path.GetFileName(filePath)
                            + " | bullet paragraphs: " + bulletParagraphsInFile
                            + " | bullet lists transformed: " + bulletListsCreatedInFile
                            + " | a_NOpagebreak created: " + noPageBreakCreatedInFile;

                        report.BulletListTransformationDetails.Add(detail);
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Bullet list transformation failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        private bool TransformContainer(
            XElement container,
            ref int bulletParagraphsDetected,
            ref int bulletListsCreated,
            ref int noPageBreakBlocksCreated)
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

                if (IsIgnorableEmptyParagraph(current) && HasNextBulletParagraph(children, index))
                {
                    index++;
                    continue;
                }

                if (IsBulletParagraph(current))
                {
                    XNamespace ns = current.Name.Namespace;

                    List<XElement> bulletParagraphs = new List<XElement>();

                    while (index < children.Count)
                    {
                        XElement nextElement = children[index];

                        if (IsIgnorableEmptyParagraph(nextElement))
                        {
                            index++;
                            continue;
                        }

                        if (!IsBulletParagraph(nextElement))
                        {
                            break;
                        }

                        bulletParagraphs.Add(nextElement);
                        index++;
                    }

                    XElement bulletList = BuildBulletList(ns, bulletParagraphs);

                    bulletParagraphsDetected += bulletParagraphs.Count;
                    bulletListsCreated++;

                    XElement introParagraph = null;

                    bool shouldWrapWithNoPageBreak =
                        bulletParagraphs.Count <= NoPageBreakThreshold
                        && TryPopIntroParagraph(newNodes, out introParagraph);

                    if (shouldWrapWithNoPageBreak && introParagraph != null)
                    {
                        XElement wrapper = new XElement(ns + "div");
                        wrapper.SetAttributeValue("class", "a_NOpagebreak");

                        wrapper.Add(CloneParagraphWithoutClass(introParagraph));
                        wrapper.Add(bulletList);

                        newNodes.Add(wrapper);
                        noPageBreakBlocksCreated++;
                    }
                    else
                    {
                        newNodes.Add(bulletList);
                    }

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

        private XElement BuildBulletList(XNamespace ns, List<XElement> bulletParagraphs)
        {
            XElement rootList = new XElement(ns + "ul");

            Dictionary<int, XElement> lastListItemByLevel = new Dictionary<int, XElement>();

            foreach (XElement bulletParagraph in bulletParagraphs)
            {
                int level = GetBulletLevel(bulletParagraph);

                if (level < 1)
                {
                    level = 1;
                }

                XElement listItem = new XElement(ns + "li");
                listItem.Add(CloneParagraphWithoutClass(bulletParagraph));

                if (level == 1 || !lastListItemByLevel.ContainsKey(level - 1))
                {
                    rootList.Add(listItem);
                    lastListItemByLevel[1] = listItem;
                    RemoveDeeperLevels(lastListItemByLevel, 1);
                    continue;
                }

                XElement parentListItem = lastListItemByLevel[level - 1];
                XElement nestedList = GetOrCreateNestedList(parentListItem, ns);

                nestedList.Add(listItem);
                lastListItemByLevel[level] = listItem;
                RemoveDeeperLevels(lastListItemByLevel, level);
            }

            return rootList;
        }

        private XElement GetOrCreateNestedList(XElement listItem, XNamespace ns)
        {
            XElement nestedList = listItem
                .Elements()
                .LastOrDefault(element => element.Name.LocalName.Equals("ul", StringComparison.OrdinalIgnoreCase));

            if (nestedList == null)
            {
                nestedList = new XElement(ns + "ul");
                listItem.Add(nestedList);
            }

            return nestedList;
        }

        private void RemoveDeeperLevels(Dictionary<int, XElement> lastListItemByLevel, int currentLevel)
        {
            List<int> deeperLevels = lastListItemByLevel
                .Keys
                .Where(level => level > currentLevel)
                .ToList();

            foreach (int level in deeperLevels)
            {
                lastListItemByLevel.Remove(level);
            }
        }

        private bool TryPopIntroParagraph(List<XNode> newNodes, out XElement introParagraph)
        {
            introParagraph = null;

            if (newNodes.Count == 0)
            {
                return false;
            }

            XElement lastElement = newNodes[newNodes.Count - 1] as XElement;

            if (lastElement == null)
            {
                return false;
            }

            if (!IsIntroParagraph(lastElement))
            {
                return false;
            }

            introParagraph = lastElement;
            newNodes.RemoveAt(newNodes.Count - 1);

            return true;
        }

        private bool IsIntroParagraph(XElement element)
        {
            if (!IsParagraph(element))
            {
                return false;
            }

            if (IsBulletParagraph(element))
            {
                return false;
            }

            if (HasClass(element, "a_action")
                || HasClass(element, "a_action_b")
                || HasClass(element, "a_action_num")
                || HasClass(element, "a_resultat")
                || HasClass(element, "a_resultat_b")
                || HasClass(element, "a_figure")
                || HasClass(element, "A_FIGURE")
                || HasClass(element, "a_normal_centered")
                || HasClass(element, "A_NORMAL_centered"))
            {
                return false;
            }

            if (IsIgnorableEmptyParagraph(element))
            {
                return false;
            }

            return true;
        }

        private bool HasDirectBulletChild(XElement element)
        {
            return element
                .Elements()
                .Any(IsBulletParagraph);
        }

        private bool HasNextBulletParagraph(List<XElement> children, int currentIndex)
        {
            for (int i = currentIndex + 1; i < children.Count; i++)
            {
                if (IsIgnorableEmptyParagraph(children[i]))
                {
                    continue;
                }

                return IsBulletParagraph(children[i]);
            }

            return false;
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

        private int GetBulletLevel(XElement element)
        {
            if (HasClass(element, "a_tiret_retrait_3"))
            {
                return 3;
            }

            if (HasClass(element, "a_tiret_retrait_2"))
            {
                return 2;
            }

            return 1;
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

            if (!string.IsNullOrEmpty(text))
            {
                return false;
            }

            return true;
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
    }
}