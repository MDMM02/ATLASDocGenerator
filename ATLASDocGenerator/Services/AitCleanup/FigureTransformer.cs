using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class FigureTransformer
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

                    int figuresInFile = 0;
                    bool changed = false;

                    List<XElement> containers = document
                        .Descendants()
                        .Where(HasDirectFigureCandidate)
                        .ToList();

                    foreach (XElement container in containers)
                    {
                        bool containerChanged = TransformContainer(container, filePath, report, ref figuresInFile);

                        if (containerChanged)
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        document.Save(filePath, SaveOptions.DisableFormatting);

                        report.FiguresTransformed += figuresInFile;

                        report.FigureTransformationDetails.Add(
                            Path.GetFileName(filePath) + " | figures transformed: " + figuresInFile
                        );
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Figure transformation failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        private bool TransformContainer(XElement container, string filePath, CleanupReport report, ref int figuresInFile)
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

                if (IsFigureCaptionParagraph(current))
                {
                    int imageIndex = FindNextImageParagraphIndex(children, index + 1);

                    if (imageIndex >= 0)
                    {
                        XElement imageParagraph = children[imageIndex];

                        XNamespace ns = current.Name.Namespace;

                        XElement figureDiv = new XElement(ns + "div");
                        figureDiv.SetAttributeValue("class", "a_figure");

                        figureDiv.Add(CloneParagraphWithoutClass(current));
                        figureDiv.Add(CloneParagraphWithoutClass(imageParagraph));

                        newNodes.Add(figureDiv);

                        figuresInFile++;
                        changed = true;

                        index = imageIndex + 1;
                        continue;
                    }

                    report.Warnings.Add(
                        "Figure caption found without following centered image: "
                        + Path.GetFileName(filePath)
                    );
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

        private int FindNextImageParagraphIndex(List<XElement> children, int startIndex)
        {
            for (int i = startIndex; i < children.Count; i++)
            {
                XElement candidate = children[i];

                if (IsIgnorableEmptyParagraph(candidate))
                {
                    continue;
                }

                if (IsCenteredImageParagraph(candidate))
                {
                    return i;
                }

                return -1;
            }

            return -1;
        }

        private bool HasDirectFigureCandidate(XElement element)
        {
            return element
                .Elements()
                .Any(IsFigureCaptionParagraph);
        }

        private bool IsFigureCaptionParagraph(XElement element)
        {
            return IsParagraph(element)
                && (HasClass(element, "a_figure") || HasClass(element, "A_FIGURE"));
        }

        private bool IsCenteredImageParagraph(XElement element)
        {
            if (!IsParagraph(element))
            {
                return false;
            }

            bool hasCenteredClass =
                HasClass(element, "a_normal_centered")
                || HasClass(element, "A_NORMAL_centered")
                || HasClass(element, "a_centre");

            if (!hasCenteredClass)
            {
                return false;
            }

            return element
                .Descendants()
                .Any(descendant => descendant.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));
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

        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        private XElement CloneParagraphWithoutClass(XElement paragraph)
        {
            XElement clone = new XElement(paragraph);

            XAttribute classAttribute = clone.Attribute("class");

            if (classAttribute != null)
            {
                classAttribute.Remove();
            }

            XAttribute styleAttribute = clone.Attribute("style");

            if (styleAttribute != null)
            {
                styleAttribute.Remove();
            }

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