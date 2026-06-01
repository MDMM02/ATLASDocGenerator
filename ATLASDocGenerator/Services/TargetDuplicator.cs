using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services
{
    public class TargetDuplicator
    {
        private static readonly XNamespace MadCapNs =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        public string DuplicateAndUpdateTarget(
            string projectRoot,
            string folderName,
            string safeReference,
            string range,
            string device,
            string fullTitle)
        {
            string sourceTargetPath = Path.Combine(projectRoot, "Project", "Targets", "Doc_SAV.fltar");
            string targetTargetPath = Path.Combine(projectRoot, "Project", "Targets", folderName + ".fltar");

            if (!File.Exists(sourceTargetPath))
                throw new Exception("Target template introuvable :\n" + sourceTargetPath);

            if (File.Exists(targetTargetPath))
                throw new Exception("Une target existe déjà avec ce nom :\n" + targetTargetPath);

            File.Copy(sourceTargetPath, targetTargetPath);

            XDocument document;

            try
            {
                document = XDocument.Load(targetTargetPath, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Impossible de lire la target copiée comme XML :\n" +
                    targetTargetPath + "\n\n" +
                    "Détail : " + ex.Message
                );
            }

            UpdateTarget(document, folderName, safeReference, range, device, fullTitle);

            document.Save(targetTargetPath);

            return targetTargetPath;
        }

        private void UpdateTarget(
            XDocument document,
            string folderName,
            string safeReference,
            string range,
            string device,
            string fullTitle)
        {
            if (document.Root == null)
                throw new Exception("La target ne contient pas d'élément racine.");

            RemoveTargetConditions(document);

            document.Root.SetAttributeValue(
                "MasterToc",
                "/Project/TOCs/" + folderName + ".fltoc"
            );

            document.Root.SetAttributeValue(
                "MasterStylesheet",
                GetStylesheetPath(range)
            );

            XElement variablesElement = document.Root.Element("Variables");

            if (variablesElement == null)
            {
                variablesElement = new XElement("Variables");
                document.Root.Add(variablesElement);
            }

            SetOrCreateVariable(variablesElement, "General/dispositif", device);
            SetOrCreateVariable(variablesElement, "General/GuideType", fullTitle);
            SetOrCreateVariable(variablesElement, "General/DocumentReference", safeReference);
        }

        private void SetOrCreateVariable(XElement variablesElement, string variableName, string value)
        {
            XElement variable = variablesElement
                .Elements("Variable")
                .FirstOrDefault(v =>
                {
                    XAttribute nameAttribute = v.Attribute("Name");
                    return nameAttribute != null && nameAttribute.Value == variableName;
                });

            if (variable == null)
            {
                variable = new XElement("Variable");
                variable.SetAttributeValue("Name", variableName);
                variablesElement.Add(variable);
            }

            variable.Value = value ?? string.Empty;
        }

        private void RemoveTargetConditions(XDocument document)
        {
            if (document.Root == null)
                return;

            foreach (XElement element in document.Root.DescendantsAndSelf())
            {
                RemoveConditionsFromAttribute(element, "conditions");
                RemoveConditionsFromAttribute(element, MadCapNs + "conditions");
            }
        }

        private void RemoveConditionsFromAttribute(XElement element, XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
                return;

            string[] conditionsToRemove =
            {
                "Stago_Gestion.Contenu commun",
                "Stago_Gestion.Commun_Tech"
            };

            string[] remainingConditions = attribute.Value
                .Split(',')
                .Select(condition => condition.Trim())
                .Where(condition => !conditionsToRemove.Contains(condition))
                .Where(condition => !string.IsNullOrWhiteSpace(condition))
                .ToArray();

            if (remainingConditions.Length == 0)
                attribute.Remove();
            else
                attribute.Value = string.Join(",", remainingConditions);
        }

        private string GetStylesheetPath(string range)
        {
            if (string.Equals(range, "STA", StringComparison.OrdinalIgnoreCase))
                return "/Content/Resources/Stylesheets/Styles_STA.css";

            return "/Content/Resources/Stylesheets/Styles.css";
        }
    }
}