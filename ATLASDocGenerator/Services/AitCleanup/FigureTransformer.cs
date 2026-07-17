using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe transforme les figures importées depuis AIT.
    /// Dans les fichiers importés, une figure ezt souvent composée de 2 parag séparés:
    /// - un paragraphe de légende avec la classe a_figure ou A_FIGURE
    /// - un paragraphe d'image centrée avec la classe a_normal_centered ou a_centre
    /// Cette classe va regrouper ces deux éléments ds un blocdiv.a_figure.
    /// </summary>
    public class FigureTransformer
    {
        /// <summary>
        /// Lance la transformation des figures 
        /// Traitement:
        /// 1. Charge chaque fichier htm comme doc XML
        /// 2. Repère les conteneurs qui ont directement une légende de figure
        /// 3. Cherche l'image centrée aui suit la légende
        /// 4. Regroupe la légende et l'image ds un div.a_figure
        /// 5. Sauvegarde le fichier ssi une modif a été faite
        /// 6. Alimente le rapport avec compteurs et details de transformation
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
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int figuresInFile = 0;
                    bool changed = false;
                    
                    // On cible uniquement les conteneurs qui ont directement une légende de figure
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

        /// <summary>
        ///  Transforme un conteneur htm qui contient une ou plusieurs légendes de figure.
        ///  Cette méthode reconstruit les enfants du conteneur:
        ///  - if légende + image centrée => div.a_figure
        ///  - if no image après légende, warning ajouté au rapport
        /// </summary>
        /// <param name="container"></param> Elément parents contenant les éléments à transformer.
        /// <param name="filePath"></param>Chemin du fichier en cours
        /// <param name="report"></param>
        /// <param name="figuresInFile"></param>
        /// <returns></returns>
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

                //Si l'élément courant est une légendre de figure, alors on cherche l'image centrée qui la suit.
                if (IsFigureCaptionParagraph(current))
                {
                    int imageIndex = FindNextImageParagraphIndex(children, index + 1);

                    if (imageIndex >= 0)
                    {
                        XElement imageParagraph = children[imageIndex];

                        XNamespace ns = current.Name.Namespace;

                        // Création du bloc figure final, la class a_figure sera utilisée
                        XElement figureDiv = new XElement(ns + "div");
                        figureDiv.SetAttributeValue("class", "a_figure");

                        figureDiv.Add(CloneParagraphWithoutClass(current));
                        figureDiv.Add(CloneParagraphWithoutClass(imageParagraph));

                        newNodes.Add(figureDiv);

                        figuresInFile++;
                        changed = true;

                        // On saute directement après l'image déja consommée
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

        /// <summary>
        /// Cherche la prochaine image centrée après une légende de figure.
        /// Si le premier élément non vide (parag vides ignorés) n'est pas une image centrée, méthode considère que la figure n'est pas valide
        /// </summary>
        /// <param name="children"></param> Liste des enfants du conteneur
        /// <param name="startIndex"></param>Index à partir duquel commencer la recherche.
        /// <returns></returns> Index de l'image centrée trouvée, ou -1 si aucune image valide n'est trouvée
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
        // Vérifie si un élément contient directement une légende de figure
        private bool HasDirectFigureCandidate(XElement element)
        {
            return element
                .Elements()
                .Any(IsFigureCaptionParagraph);
        }

        // Vérifie si un paragraphe correspond à une légende de figure
        private bool IsFigureCaptionParagraph(XElement element)
        {
            return IsParagraph(element)
                && (HasClass(element, "a_figure") || HasClass(element, "A_FIGURE"));
        }

        // Vérifie si un paragraphe correspond à une image centrée
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

        // Vérifie si un paragraphe est vide et peut être ignoré
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