using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class SimpleStyleCleanupTransformer
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

                    int cleanedInFile = 0;

                    List<XElement> elements = document
                        .Descendants()
                        .ToList();

                    foreach (XElement element in elements)
                    {
                        if (element.Parent == null)
                        {
                            continue;
                        }

                        bool changed = CleanupElement(element);

                        if (changed)
                        {
                            cleanedInFile++;
                        }
                    }

                    if (cleanedInFile > 0)
                    {
                        document.Save(filePath);

                        report.StylesCleaned += cleanedInFile;

                        report.StyleCleanupDetails.Add(
                            Path.GetFileName(filePath) + " | simple styles cleaned: " + cleanedInFile
                        );
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Simple style cleanup failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        private bool CleanupElement(XElement element)
        {
            if (IsSubscriptSpan(element))
            {
                ReplaceSpanWithElement(element, "sub");
                return true;
            }

            if (IsSuperscriptSpan(element))
            {
                ReplaceSpanWithElement(element, "sup");
                return true;
            }

            if (IsParagraph(element))
            {
                if (HasClass(element, "a_normal_centered") || HasClass(element, "A_NORMAL_centered"))
                {
                    SetSingleClass(element, "a_centre");
                    return true;
                }

                if (RemoveSimpleParagraphClasses(element))
                {
                    return true;
                }
            }

            if (IsHeading(element))
            {
                if (RemoveHeadingClasses(element))
                {
                    return true;
                }
            }

            return false;
        }

        private bool RemoveSimpleParagraphClasses(XElement element)
        {
            return RemoveClasses(
                element,
                new[]
                {
                    "a_normal",
                    "A_NORMAL",
                    "a_menu",
                    "A_MENU",
                    "a_ref",
                    "A_REF",
                    "a_souligne",
                    "A_SOULIGNE",
                    "a_normal_revision",
                    "A_NORMAL_revision",
                    "a_mqt_vide",
                    "A_MQT_VIDE",
                    "a_mqt_videencradre",
                    "A_MQT_VIDEENCRADRE"
                }
            );
        }

        private bool RemoveHeadingClasses(XElement element)
        {
            return RemoveClasses(
                element,
                new[]
                {
                    "heading1",
                    "heading2",
                    "heading3",
                    "heading4",
                    "heading5",
                    "heading6"
                }
            );
        }

        private bool IsSubscriptSpan(XElement element)
        {
            return IsSpan(element)
                && (HasClass(element, "ZZZZSubscript") || HasClass(element, "subscript"));
        }

        private bool IsSuperscriptSpan(XElement element)
        {
            return IsSpan(element)
                && (HasClass(element, "ZZZZSuperscript") || HasClass(element, "superscript"));
        }

        private void ReplaceSpanWithElement(XElement span, string newElementName)
        {
            XNamespace ns = span.Name.Namespace;

            XElement replacement = new XElement(ns + newElementName);

            foreach (XNode node in span.Nodes())
            {
                replacement.Add(CloneNode(node));
            }

            span.ReplaceWith(replacement);
        }

        private XNode CloneNode(XNode node)
        {
            XElement element = node as XElement;

            if (element != null)
            {
                return new XElement(element);
            }

            XText text = node as XText;

            if (text != null)
            {
                return new XText(text.Value);
            }

            XCData cdata = node as XCData;

            if (cdata != null)
            {
                return new XCData(cdata.Value);
            }

            XComment comment = node as XComment;

            if (comment != null)
            {
                return new XComment(comment.Value);
            }

            return new XText(node.ToString());
        }

        private void SetSingleClass(XElement element, string className)
        {
            element.SetAttributeValue("class", className);
        }

        private bool RemoveClasses(XElement element, string[] classesToRemove)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
            {
                return false;
            }

            List<string> existingClasses = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            List<string> remainingClasses = existingClasses
                .Where(existingClass =>
                    !classesToRemove.Any(classToRemove =>
                        existingClass.Equals(classToRemove, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (remainingClasses.Count == existingClasses.Count)
            {
                return false;
            }

            if (remainingClasses.Count == 0)
            {
                classAttribute.Remove();
            }
            else
            {
                classAttribute.Value = string.Join(" ", remainingClasses.ToArray());
            }

            return true;
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

        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSpan(XElement element)
        {
            return element.Name.LocalName.Equals("span", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsHeading(XElement element)
        {
            string localName = element.Name.LocalName;

            return localName.Equals("h1", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("h2", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("h3", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("h4", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("h5", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("h6", StringComparison.OrdinalIgnoreCase);
        }
    }
}