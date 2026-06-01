using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services
{
    public class TocDuplicator
    {
        public string DuplicateAndUpdateToc(
            string projectRoot,
            string folderName,
            string safeReference)
        {
            string sourceTocPath = Path.Combine(projectRoot, "Project", "TOCs", "Doc_SAV.fltoc");
            string targetTocPath = Path.Combine(projectRoot, "Project", "TOCs", folderName + ".fltoc");

            if (!File.Exists(sourceTocPath))
            {
                throw new Exception("TOC template introuvable :\n" + sourceTocPath);
            }

            if (File.Exists(targetTocPath))
            {
                throw new Exception("Une TOC existe déjà avec ce nom :\n" + targetTocPath);
            }

            File.Copy(sourceTocPath, targetTocPath);

            XDocument document;

            try
            {
                document = XDocument.Load(targetTocPath, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Impossible de lire la TOC copiée comme XML :\n" +
                    targetTocPath + "\n\n" +
                    "Détail : " + ex.Message
                );
            }

            RemoveTocConditions(document);
            UpdateTocLinks(document, folderName, safeReference);

            document.Save(targetTocPath);

            return targetTocPath;
        }

        private void RemoveTocConditions(XDocument document)
        {
            if (document.Root == null)
                return;

            foreach (XElement element in document.Root.DescendantsAndSelf())
            {
                RemoveConditionsFromAttribute(element, "conditions");
                RemoveConditionsFromAttribute(element, XName.Get("conditions", "http://www.madcapsoftware.com/Schemas/MadCap.xsd"));
            }
        }

        private void RemoveConditionsFromAttribute(XElement element, XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
                return;

            string[] conditionsToRemove = new string[]
            {
                "Stago_Gestion.Contenu commun",
                "Stago_Gestion.40_DoNotTranslate",
                "Stago_Gestion.Commun_Tech"
            };

            string[] remainingConditions = attribute.Value
                .Split(',')
                .Select(condition => condition.Trim())
                .Where(condition => !conditionsToRemove.Contains(condition))
                .Where(condition => !string.IsNullOrWhiteSpace(condition))
                .ToArray();

            if (remainingConditions.Length == 0)
            {
                attribute.Remove();
            }
            else
            {
                attribute.Value = string.Join(",", remainingConditions);
            }
        }

        private void UpdateTocLinks(XDocument document, string folderName, string safeReference)
        {
            Dictionary<string, string> replacements = BuildLinkReplacementMap(folderName, safeReference);

            foreach (XElement element in document.Descendants())
            {
                XAttribute linkAttribute = element.Attribute("Link");

                if (linkAttribute == null)
                    continue;

                string oldLink = linkAttribute.Value;

                if (replacements.ContainsKey(oldLink))
                {
                    linkAttribute.Value = replacements[oldLink];
                }
            }
        }

        private Dictionary<string, string> BuildLinkReplacementMap(string folderName, string safeReference)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            map.Add(
                "/Content/Template_tech/Title_doc.htm",
                "/Content/" + folderName + "/Title_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Resources/Commun Stago/topics_Tech/Historique_tech.htm",
                "/Content/" + folderName + "/Historique_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Objectif.htm",
                "/Content/" + folderName + "/Objectif_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Mesures de sécurité.htm",
                "/Content/" + folderName + "/Mesures_securite_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Matériel nécessaire.htm",
                "/Content/" + folderName + "/Materiel_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Documents nécessaires.htm",
                "/Content/" + folderName + "/Documents_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Prérequis.htm",
                "/Content/" + folderName + "/Prerequis_" + safeReference + ".htm"
            );

            map.Add(
                "/Content/Template_tech/1er_chapitre.htm",
                "/Content/" + folderName + "/1er_chapitre.htm"
            );

            return map;
        }
    }
}