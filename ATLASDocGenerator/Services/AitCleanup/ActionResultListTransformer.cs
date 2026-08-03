using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe transforme les paragraphes Action/Résultat importés en paragraphes AIT.
    /// 
    /// Elle reccherche les paragraphes avec les classes:
    /// - a_action_num pour les actions numérotées
    /// - a_action et a_action_b pour les actions à puces
    /// - a_result et a_result_b pour les résultats associés
    /// 
    /// Elle regroupe ces paragraphes dans les listes propres:
    /// - ol.Action_num pour les actions numérotées
    /// - ul.Action_bullet pour les actions à puces
    /// - ul imbriquée dans chaque li pour les résultats associés à une action
    /// </summary>
    public class ActionResultListTransformer
    {
        /// <summary>
        ///  Lance la transformation A/R sur tous les fichiers .htm fournis.
        ///  Traitement:
        ///  1. Charge le fichier .htm comme document XML
        ///  2. Repère les conteneurs qui ont des enfants A/R directs
        ///  3. Transforme les suites de paragraphes en list htm
        ///  4. Sauvegarde le fichier si une modification a été faite
        ///  5. Alimente le rapport avec les détails de la transformation
        /// </summary>

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
                    // Charge le fichier .htm en conservant les espaces existants (important pr ne pas modifier le fichier inutilement).
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int actionNumDetectedInFile = 0;
                    int actionBulletDetectedInFile = 0;
                    int resultDetectedInFile = 0;
                    int listsCreatedInFile = 0;

                    bool changed = false;
                    // On ne transforme que les conteneurs qui ont directement des paragraphes A/R (évite de parcourir et modifier inutilement la hierarchie du doc).

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
                    { // Sauvegarde le doc s'il a été modifié
                        FileBackupService.CreateInitialBackup(
                            filePath,
                            ".before-ait-cleanup.bak");

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
                    // MAJ des compteurs globaux du rapport.
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

        /// <summary>
        ///  Transforme un conteneur htm qui contient directement des paragraphes A/R.
        ///  Principe:
        ///  - une suite de a_action_num est transformée en liste ol.Action_num
        ///  - une suite de a_action ou a_action_b est transformée en liste ul.Action_bullet
        ///  - !les paragraphes Résultat qui suivent une action sont ajoutés dans une sous liste
        ///  - Les images centrées qui suvent une action sont conservées ds le même élément de liste
        /// </summary>
        /// <param name="container"></param> Elément parent contenant les parag à transformer
        /// <param name="actionNumDetected"></param> Compteur des actions numérotées detectées
        /// <param name="actionBulletDetected"></param> Compteur des actins à puces detectées
        /// <param name="resultDetected"></param> Compteur des résultats detectés
        /// <param name="listsCreated"></param> Compteur des listes créées
        /// <returns></returns> Retourne True si le conteneur a été modifié , sinon false
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

                if (TryGetActionKind(current, out actionKind)) // Si l'élément est une action, on démarre une nouvelle liste.
                {
                    XNamespace ns = current.Name.Namespace;

                    XElement listElement;
                    // Les actions numérotées deviennent une liste ol
                    if (actionKind == "numbered")
                    {
                        listElement = new XElement(ns + "ol");
                        listElement.SetAttributeValue("class", "Action_num");
                    }
                    else
                    { // Les actions à puces deviennent une liste ul
                        listElement = new XElement(ns + "ul");
                        listElement.SetAttributeValue("class", "Action_bullet");
                    }
                    // On regroupe toutes les actions consécutives du même type
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

                        // On clone le paragraphe d'action dans sa classe AIT d'origine
                        XElement actionParagraph = CloneParagraphWithoutClass(actionElement);
                        listItem.Add(actionParagraph);

                        index++;

                        XElement resultList = null;
                        // On rattache à l'action les résultats ou images qui la suivent directement
                        while (index < children.Count)
                        {
                            XElement nextElement = children[index];
                            // Paragraphes vides ignorés
                            if (IsIgnorableEmptyParagraph(nextElement))
                            {
                                index++;
                                continue;
                            }
                            // Les résultats deviennent des li dans une sous liste
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
                            // Les images centrées qui suivent une action sont conservées dans le même li que l'action
                            if (IsCenteredImageParagraph(nextElement))
                            {
                                XElement imageParagraph = CloneParagraphWithClass(nextElement, "a_centre");
                                listItem.Add(imageParagraph);

                                index++;
                                continue;
                            }
                            // Dès qu'on tombe sur un autre élément, on arrète la séquence
                            break;
                        }

                        listElement.Add(listItem);
                    }

                    newNodes.Add(listElement);
                    listsCreated++;
                    changed = true;
                    continue;
                }
                // si l'élément n'est ps une action, on le garde tel quel
                newNodes.Add(new XElement(current));
                index++;
            }

            if (changed)
            {
                // Remplace le contenu du conteneur par la nouvelle structure transformée
                container.ReplaceNodes(newNodes);
            }

            return changed;
        }
        /// <summary>
        /// Vérifie si un élément contient directement au moins un enfant A/R (action ou résultat).
        /// 
        /// Cela permet d'identifier les conteneurs réellement concernées par la transformzation.
        /// </summary>
        /// <param name="element"></param> Element XML à analyser
        /// <returns></returns> True si un enfant direct est une action ou un resultat, sinon false
        private bool HasDirectActionOrResultChild(XElement element)
        {
            return element
                .Elements()
                .Any(child =>
                    TryGetActionKind(child, out _) ||
                    IsResultParagraph(child)
                );
        }
        
        /// <summary>
        ///  Determine si un élément est un praragraphe d'action.
        ///  Si l'actin est détectée, actionKind vaut:
        ///  - "numbered" pour a_action_num
        ///  - "bullet" pour a_action ou a_action_b
        /// </summary>
        /// <param name="element"></param>
        /// <param name="actionKind"></param> type d'action détecté
        /// <returns></returns> True si l'element est une action, sinon false
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
        /// <summary>
        /// Vérifie si un élément est un paragraphe Résultat.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns> True si l'éléossède une classe resultat, sinon false.
        private bool IsResultParagraph(XElement element)
        {
            return IsParagraph(element)
                && (HasClass(element, "a_resultat") || HasClass(element, "a_resultat_b"));
        }

        /// <summary>
        /// Vérifie si un paragraphe correspond à une image centrée importée depuis AIT
        /// Images conservées ds l'élément de liste de l'action précédente.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Vérifie si l'élément XML est un paragraphe
        /// Test qui utilise LocalName pr éviter les pb de namespace
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clone un paragraphe et supprime son attribut class.
        /// Permet de retirer les classes AIT d'origine après transformation.
        /// </summary>
        /// <param name="paragraph"></param> Paragraphe à cloner
        /// <returns></returns> Clone du paragraph sans attribut class
        private XElement CloneParagraphWithoutClass(XElement paragraph)
        {
            XElement clone = new XElement(paragraph);
            clone.Attribute("class")?.Remove();
            return clone;
        }

        /// <summary>
        /// Clone un paragraphe et force une nouvelle classe CSS.
        /// Utilisé pour conserver les images centrées avec une classe propre.
        /// </summary>
        /// <param name="paragraph"></param> Paragraphe à cloner
        /// <param name="className"></param> Classe CSS à appliquer au clone
        /// <returns></returns> Clone du paragraphe avec la classe demandée
        private XElement CloneParagraphWithClass(XElement paragraph, string className)
        {
            XElement clone = new XElement(paragraph);
            clone.SetAttributeValue("class", className);
            return clone;
        }

        /// <summary>
        /// Vérifie si un élément XML possède une classe CSS donnée, ou plusieurs
        /// </summary>
        /// <param name="element"></param> element du xml
        /// <param name="className"></param> classe CSS recherchée
        /// <returns></returns> True si la classe est présente, sinon false
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

        /// <summary>
        /// Vérifie si un paragraphe est vide et peut être ignoré pdnt la transformation.
        /// Les paragraphes sont ignorés entre une action et ses résultats pour ne ps couper artificiellement la séquence.
        /// Un paragraphe contenant une image n'est jamais considéré comme ignorable.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
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
