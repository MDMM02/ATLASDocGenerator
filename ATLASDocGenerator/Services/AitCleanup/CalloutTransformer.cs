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
    /// 
    /// Cette classe transforme les callouts importés
    /// Les encadrés Information / Précaution / Attention (IPA) arrivent ss forme de tableaux avec une cellule contenant une icone et une cellule contenant le texte de l'encadré
    /// 
    /// Cette classe remplace ces tableaux par des div plus propres:
    /// - div.a_Information
    /// - div.a_Precaution
    /// - div.a_Attention
    /// Elle supprime les parag d'espacement inutiles placés avant les callouts
    /// </summary>
    public class CalloutTransformer
    {
        /// <summary>
        /// Lance la transformation des callouts sur tous les fichiers htm fournis
        /// Traitement:
        /// 1. Charge chaque fichier htm comme doc xml
        /// 2. Repère les conteneurs qui ont directement un tableau ou un parag d'espacement de callout
        /// 3. Transforme les tableaux de callout en div avec la bonne classe
        /// 4. Supprime les paragrpahes d'espacement inutiles avant les callouts
        /// 5. Sauvegarde le fichier seulement si une mdoif a été faite
        /// 6. Alimente le compteur
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
                    // Charge le fichier en conservant les espaces existants, évite de reformatter tout le fichier inutilement
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int calloutsInFile = 0;
                    bool changed = false;
                    // On cible uniquement les conteneurs qui ont directement un tableau ou un parag d'espacement lié aux callouts
                    List<XElement> containers = document
                        .Descendants()
                        .Where(HasDirectCalloutCandidate)
                        .ToList();

                    foreach (XElement container in containers)
                    {
                        bool containerChanged = TransformContainer(container, filePath, report, ref calloutsInFile);

                        if (containerChanged)
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        FileBackupService.CreateInitialBackup(
                            filePath,
                            ".before-ait-cleanup.bak");

                        document.Save(filePath, SaveOptions.DisableFormatting);

                        report.CalloutsTransformed += calloutsInFile;

                        report.CalloutTransformationDetails.Add(
                            Path.GetFileName(filePath) + " | callouts transformed: " + calloutsInFile
                        );
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Callout transformation failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Transforme un conteneur HTML qui contient directement des candidats callout.
        /// 
        /// Cette méthode reconstruit les enfants du conteneur :
        /// - les paragraphes d'espacement avant callout sont supprimés
        /// - les tableaux de callout sont remplacés par des div propres
        /// - les autres éléments sont conservés tels quels
        /// </summary>
        /// <param name="container">Élément parent contenant les éléments à transformer.</param>
        /// <param name="filePath">Chemin du fichier en cours, utilisé pour le rapport.</param>
        /// <param name="report">Rapport de nettoyage à compléter.</param>
        /// <param name="calloutsInFile">Compteur des callouts transformés dans le fichier.</param>
        /// <returns>True si le conteneur a été modifié, sinon false.</returns>
        private bool TransformContainer(XElement container, string filePath, CleanupReport report, ref int calloutsInFile)
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
                
                // Supprime les paragraphes d'espacement placés juste avant un tableau de callout
                if (IsCalloutSpacingParagraph(current) && HasNextCalloutTable(children, index))
                {
                    index++;
                    changed = true;
                    continue;
                }

                XElement calloutDiv;
                
                // Si l'élément courant est un tableau de callout reconnu -> transforme en div IPA
                if (TryBuildCalloutDiv(current, filePath, report, out calloutDiv))
                {
                    newNodes.Add(calloutDiv);
                    calloutsInFile++;
                    changed = true;
                    index++;
                    continue;
                }
                // Si élément pas concerné, alors on le laisse tel quel
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
        /// Tente de transformer un tableau htm en div.
        /// On attend un tableau, au moins deux cellules, icone et texte
        /// </summary>
        /// <param name="table"></param> Elément potentiellement transformable en callout
        /// <param name="filePath"></param>
        /// <param name="report"></param>
        /// <param name="calloutDiv"></param>Div de callout généréé si la transformation réussit.
        /// <returns></returns>
        private bool TryBuildCalloutDiv(XElement table, string filePath, CleanupReport report, out XElement calloutDiv)
        {
            calloutDiv = null;

            if (!IsTable(table))
            {
                return false;
            }
            // Récupère la 1ere lignedu tableau
            XElement firstRow = table
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName.Equals("tr", StringComparison.OrdinalIgnoreCase));

            if (firstRow == null)
            {
                return false;
            }
            // Repère les cellules de la 1ere ligne
            List<XElement> cells = firstRow
                .Elements()
                .Where(element =>
                    element.Name.LocalName.Equals("td", StringComparison.OrdinalIgnoreCase)
                    || element.Name.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cells.Count < 2)
            {
                return false;
            }

            XElement iconCell = cells[0]; // Cellule qui contient l'icone
            XElement contentCell = cells[1]; // Contient le texte

            // Le tableau est considéré comme un callout seulement si la 1ere cellule contient une image (logique à changer?)
            XElement iconImage = iconCell
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));

            if (iconImage == null)
            {
                return false;
            }

            string iconSource = GetAttributeValue(iconImage, "src");
            bool usedFallback;

            // Déterminela classe du callout à partir de l'icône ou du style du tableau
            string calloutClass = ResolveCalloutClass(iconSource, table, out usedFallback);

            XNamespace ns = table.Name.Namespace;

            XElement div = new XElement(ns + "div");
            div.SetAttributeValue("class", calloutClass);

            int contentCount = 0;

            // Copie le contenu de la 2eme cellule ds la nouvelle div.
            foreach (XElement contentElement in contentCell.Elements())
            {
                if (IsIgnorableEmptyParagraph(contentElement))
                {
                    continue;
                }

                XElement clone = new XElement(contentElement);
                CleanCalloutContentElement(clone);

                div.Add(clone);
                contentCount++;
            }

            if (contentCount == 0)
            {
                report.Warnings.Add("Callout table ignored because no content was found: " + filePath);
                return false;
            }
            // Siicône pas reconnue,on garde une trace ds le rapport.
            if (usedFallback)
            {
                report.Warnings.Add(
                    "Callout icon not recognized, defaulted to a_Information: "
                    + Path.GetFileName(filePath)
                    + " | icon src: "
                    + iconSource
                );
            }

            calloutDiv = div;
            return true;
        }
        /// <summary>
        /// Détermine la classe CSS à appliquer au callout
        /// Détection qui se base sur:
        /// - nom ou chemin de l'icône
        /// - code couleurs ds le style du tableau
        /// Si rien n'est reconnu, une classe par défaut est utilisée
        /// </summary>
        /// <param name="iconSource"></param>
        /// <param name="table"></param>
        /// <param name="usedFallback"></param>
        /// <returns></returns> Class CSS du callout
        private string ResolveCalloutClass(string iconSource, XElement table, out bool usedFallback)
        {
            usedFallback = false;

            string tableStyle = GetAttributeValue(table, "style");

            // INFORMATION = bleu
            if (ContainsIgnoreCase(iconSource, "information")
                || ContainsIgnoreCase(iconSource, "info")
                || ContainsIgnoreCase(iconSource, "Image5150")
                || ContainsIgnoreCase(tableStyle, "#0070C0")
                || ContainsIgnoreCase(tableStyle, "#005EB8")
                || ContainsIgnoreCase(tableStyle, "#4472C4")
                || ContainsIgnoreCase(tableStyle, "blue"))
            {
                return "a_Information";
            }

            // PRECAUTION = orange
            if (ContainsIgnoreCase(iconSource, "precaution")
                || ContainsIgnoreCase(iconSource, "warning")
                || ContainsIgnoreCase(iconSource, "caution")
                || ContainsIgnoreCase(tableStyle, "#B57406")
                || ContainsIgnoreCase(tableStyle, "#F4B183")
                || ContainsIgnoreCase(tableStyle, "orange"))
            {
                return "a_Precaution";
            }

            // ATTENTION = rouge
            if (ContainsIgnoreCase(iconSource, "attention")
                || ContainsIgnoreCase(iconSource, "danger")
                || ContainsIgnoreCase(iconSource, "alert")
                || ContainsIgnoreCase(tableStyle, "#C00000")
                || ContainsIgnoreCase(tableStyle, "#FF0000")
                || ContainsIgnoreCase(tableStyle, "red"))
            {
                return "a_Attention";
            }
            // Classe information qui sert de filet de sauvetage
            usedFallback = true;
            return "a_Information";
        }
        // Nettoie le contenu copié depuis la cellule de texte du callout
        private void CleanCalloutContentElement(XElement element)
        {
            if (IsParagraph(element) && !ShouldPreserveParagraphClass(element))
            {
                XAttribute classAttribute = element.Attribute("class");

                if (classAttribute != null)
                {
                    classAttribute.Remove();
                }
            }

            XAttribute styleAttribute = element.Attribute("style");

            if (styleAttribute != null)
            {
                styleAttribute.Remove();
            }

            XAttribute widthAttribute = element.Attribute("width");

            if (widthAttribute != null)
            {
                widthAttribute.Remove();
            }

            foreach (XElement descendant in element.Descendants().ToList())
            {
                if (IsParagraph(descendant) && !ShouldPreserveParagraphClass(descendant))
                {
                    XAttribute descendantClassAttribute = descendant.Attribute("class");

                    if (descendantClassAttribute != null)
                    {
                        descendantClassAttribute.Remove();
                    }
                }

                XAttribute descendantStyleAttribute = descendant.Attribute("style");

                if (descendantStyleAttribute != null)
                {
                    descendantStyleAttribute.Remove();
                }

                XAttribute descendantWidthAttribute = descendant.Attribute("width");

                if (descendantWidthAttribute != null)
                {
                    descendantWidthAttribute.Remove();
                }
            }
        }
        // Indique i la classe d'un paragraphe doit être conservée ds le callout (tirets, A/R, images...)
        private bool ShouldPreserveParagraphClass(XElement element)
        {
            return IsBulletParagraph(element)
                || HasClass(element, "a_action")
                || HasClass(element, "a_action_b")
                || HasClass(element, "a_action_num")
                || HasClass(element, "a_resultat")
                || HasClass(element, "a_resultat_b")
                || HasClass(element, "a_normal_centered")
                || HasClass(element, "A_NORMAL_centered");
        }

        private bool HasDirectCalloutCandidate(XElement element)
        {
            return element
                .Elements()
                .Any(child => IsCalloutSpacingParagraph(child) || IsTable(child));
        }

        // Vérifie s'il existe un tableau de callout après l'index courant
        private bool HasNextCalloutTable(List<XElement> children, int currentIndex)
        {
            for (int i = currentIndex + 1; i < children.Count; i++)
            {
                if (IsCalloutSpacingParagraph(children[i]) || IsIgnorableEmptyParagraph(children[i]))
                {
                    continue;
                }

                return IsPotentialCalloutTable(children[i]);
            }

            return false;
        }

        // Vérifie si un élément est un tableau pouvant correspondre à un callout (contenir une image), logique à revoir
        private bool IsPotentialCalloutTable(XElement element)
        {
            if (!IsTable(element))
            {
                return false;
            }

            return element
                .Descendants()
                .Any(descendant => descendant.Name.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTable(XElement element)
        {
            return element.Name.LocalName.Equals("table", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCalloutSpacingParagraph(XElement element)
        {
            return IsParagraph(element)
                && HasClass(element, "a_mqt_videencradre");
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

        private string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return string.Empty;
            }

            return attribute.Value;
        }

        private bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
