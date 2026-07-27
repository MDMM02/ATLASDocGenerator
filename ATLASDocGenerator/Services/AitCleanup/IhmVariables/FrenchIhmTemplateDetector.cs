using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services.AitCleanup.IhmVariables
{
    /// <summary>
    /// Détecte automatiquement les templates Topic présents dans un export XML Author-it.
    /// </summary>
    public class FrenchIhmTemplateDetector
    {
        private const string FrenchLocId = "1";

        public List<FrenchIhmTemplateInfo> Detect(string sourceXmlPath)
        {
            if (string.IsNullOrWhiteSpace(sourceXmlPath))
                throw new ArgumentException("Author-it XML source path is empty.");

            if (!File.Exists(sourceXmlPath))
                throw new FileNotFoundException(
                    "Author-it XML source file was not found.",
                    sourceXmlPath);

            XDocument document = XDocument.Load(
                sourceXmlPath,
                LoadOptions.PreserveWhitespace);

            List<XElement> topics = document
                .Descendants()
                .Where(element =>
                    string.Equals(
                        element.Name.LocalName,
                        "Topic",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<FrenchIhmTemplateInfo> detectedTemplates =
                new List<FrenchIhmTemplateInfo>();

            foreach (XElement topic in topics)
            {
                XElement objectElement = GetDirectChild(topic, "Object");

                if (!IsFrenchRootTemplate(objectElement))
                    continue;

                string templateId =
                    GetDirectChildValue(objectElement, "ID");

                string description =
                    GetDirectChildValue(objectElement, "Description");

                if (string.IsNullOrWhiteSpace(templateId))
                    continue;

                int frenchTopicCount = CountFrenchTopicsUsingTemplate(
                    topics,
                    templateId);

                // Un template qui n'est utilisé par aucun topic n'est pas utile dans la liste IHM.
                if (frenchTopicCount == 0)
                    continue;

                if (string.IsNullOrWhiteSpace(description))
                    description = "Template_" + templateId;

                detectedTemplates.Add(
                    new FrenchIhmTemplateInfo
                    {
                        Id = templateId,
                        Description = description,
                        FrenchTopicCount = frenchTopicCount
                    });
            }

            return detectedTemplates
                .OrderBy(
                    item => item.Description,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    item => item.Id,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool IsFrenchRootTemplate(XElement objectElement)
        {
            if (objectElement == null)
                return false;

            string type =
                GetDirectChildValue(objectElement, "Type");

            string isTemplate =
                GetDirectChildValue(objectElement, "IsTemplate");

            string locId =
                GetDirectChildValue(objectElement, "LocID");

            string variantParentId =
                GetDirectChildValue(objectElement, "VariantParentID");

            bool isRootVariant =
                string.IsNullOrWhiteSpace(variantParentId)
                || variantParentId == "-1"
                || variantParentId == "0";

            return string.Equals(
                       type,
                       "Topic",
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       isTemplate,
                       "true",
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       locId,
                       FrenchLocId,
                       StringComparison.OrdinalIgnoreCase)
                   && isRootVariant;
        }

        private int CountFrenchTopicsUsingTemplate(
            List<XElement> topics,
            string templateId)
        {
            int count = 0;

            foreach (XElement topic in topics)
            {
                XElement objectElement =
                    GetDirectChild(topic, "Object");

                if (objectElement == null)
                    continue;

                string type =
                    GetDirectChildValue(objectElement, "Type");

                string isTemplate =
                    GetDirectChildValue(objectElement, "IsTemplate");

                string locId =
                    GetDirectChildValue(objectElement, "LocID");

                string basedOn =
                    GetDirectChildValue(objectElement, "BasedOn");

                bool matches =
                    string.Equals(
                        type,
                        "Topic",
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        isTemplate,
                        "false",
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        locId,
                        FrenchLocId,
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        basedOn,
                        templateId,
                        StringComparison.OrdinalIgnoreCase);

                if (matches)
                    count++;
            }

            return count;
        }

        private XElement GetDirectChild(
            XElement parent,
            string localName)
        {
            if (parent == null)
                return null;

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
            XElement child =
                GetDirectChild(parent, localName);

            return child == null
                ? string.Empty
                : child.Value.Trim();
        }
    }

    /// <summary>
    /// Représente un template détecté dans le XML Author-it.
    /// </summary>
    public class FrenchIhmTemplateInfo
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public int FrenchTopicCount { get; set; }

        public override string ToString()
        {
            return Description
                + " — ID "
                + Id
                + " — "
                + FrenchTopicCount
                + " topic(s) français";
        }
    }
}