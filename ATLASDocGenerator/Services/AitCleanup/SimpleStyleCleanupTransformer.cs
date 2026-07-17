using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe efectue un nettoyage simple de styles importés depuis AIT.
    /// Elle ne reconstruit pas toute la structure du document, elle aplique uniquement des corrections ciblées:
    /// - Remplacement des spans d'indice par des balises sub
    /// - Remplacement des spans d'exposant par des balises sup
    /// - Normalisation des paragraphes centrés en classe a_centre
    /// Suppression des classes AIT simples ou parasites
    /// Suppression des classes heading1 à heading 6 sur les titres h1 à h6
    /// </summary>
    public class SimpleStyleCleanupTransformer
    {
        /// <summary>
        /// Lance le nettoyage simple des styles sur tous les fichiers .htm fournis
        /// Traitement:
        /// 1. Charge chaque fichier htm comme un doc xml
        /// 2. Parcourt tous les éléments du document
        /// 3. Applique les règles de nettoyage élément par élément
        /// 4. Sauvegarde le fichier ssi un élément a été modifié
        /// 5. Alimente le rapport avec le nb d'éléments nettoyés
        /// </summary>
        /// <param name="htmlFiles"></param>
        /// <param name="report"></param>
        /// <exception cref="ArgumentNullException"></exception>
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
                    // Charge le fichier en conservant les espaces existants
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int cleanedInFile = 0; // Nombre d'éléments nettoyés ds le fichier

                    // On crée une liste avant de modifier le document (évite pb pdnt itération)
                    List<XElement> elements = document
                        .Descendants()
                        .ToList();

                    foreach (XElement element in elements)
                    {
                        // Ignore les éléments qui n'ont plus de parent.
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

        /// <summary>
        /// 
        /// Applique règle de nettoyage sur un élément XML donné
        /// Règles appliquées :
        /// - span.ZZZZSubscript ou span.subscript devient sub
        /// - span.ZZZZSuperscript ou span.superscript devient sup
        /// - p.a_normal_centered / p.A_NORMAL_centered devient p.a_centre
        /// - certaines classes de paragraphes simples sont retirées
        /// - certaines classes de titres sont retirées
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
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
                // Normalise les paragraphes centrés vers une classe unique utilisée ds le projet
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

        /// <summary>
        /// Supprime les classes simples de paragraphes importées depuis Author-it.
        /// 
        /// Ces classes correspondent souvent à des styles de base ou à des espacements
        /// qui ne sont plus nécessaires après import dans Flare.
        /// </summary>
        /// <param name="element">Paragraphe à nettoyer.</param>
        /// <returns>True si au moins une classe a été supprimée.</returns>
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

        /// <summary>
        /// Supprime les classes heading1 à heading6 sur les titres HTML.
        /// 
        /// Après import, le niveau du titre est déjà porté par la balise h1/h2/h3.
        /// La classe headingX devient donc redondante.
        /// </summary>
        /// <param name="element">Titre h1 à h6 à nettoyer.</param>
        /// <returns>True si une classe heading a été supprimée.</returns>
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

        // Vérifie si un élément est un span d'indice
        private bool IsSubscriptSpan(XElement element)
        {
            return IsSpan(element)
                && (HasClass(element, "ZZZZSubscript") || HasClass(element, "subscript"));
        }

        // Vérifie si un élément est un span d'exposant
        private bool IsSuperscriptSpan(XElement element)
        {
            return IsSpan(element)
                && (HasClass(element, "ZZZZSuperscript") || HasClass(element, "superscript"));
        }

        // Remplace un span par une nouvelle basile
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

        // Clone un noeud XML en conservant son type, si type pas reconnu -> converti en texte
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

        // Force une classe unique sur un élément.Normalisation de certains styles AIT
        private void SetSingleClass(XElement element, string className)
        {
            element.SetAttributeValue("class", className);
        }

        // Supprime une liste de classes CSS d'un élément
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

        // Vérifie si l'élément XML est un paragraphe p
        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        // Vérifie si l'élément xml est un span
        private bool IsSpan(XElement element)
        {
            return element.Name.LocalName.Equals("span", StringComparison.OrdinalIgnoreCase);
        }

        // Vérifie si l'élément xml est un titre htm h1 à h6
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