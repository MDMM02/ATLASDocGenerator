using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe transforme les listes à puces importées.
    /// Ds les fichiers importés les puces arrivent ss forme de paragraphes successifs avec des classes comme a_tiret, a_tiret_retrait_2 ou a_tiret_retrait_3.
    /// 
    /// Cette classe les convertit en vraies listes htm:
    /// - ul pour la liste principale
    /// -li pour chaque element de liste
    /// - ul imbriquées pour les sous niveaux
    /// 
    /// Elle peut aussi regrouper un paragraphe d'introduction + courte liste ds un div.a_NOpagebreak pour éviter les coupures de page au build PDF.
    /// </summary>
    public class BulletListTransformer
    {
        private const int NoPageBreakThreshold = 8;
        /// <summary>
        /// Lance la transformation des listes à puces sur tous les fichier .htm
        /// Traitement:
        /// 1. Charge chaque .htm comme XML
        /// 2. Récupère les conteneurs qu ont directement des paragraphes de type puce
        /// 3. Transforme les paragraphes a_tiret en vraies listes htm
        /// 4. Crée éventuellement un bloc a_NOpagebreak si la liste est courte
        /// 5. Sauvegarde le fichier si modif de faite
        /// 6. Alimente le raport avec les compteurs et détails de transformation
        /// </summary>
        /// <param name="htmlFiles"></param> liste de fichier .htm à traiter
        /// <param name="report"></param> Rapport de nettoyage à compléter
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
                    // Charge le fichier en conservant les espaces existants.
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int bulletParagraphsInFile = 0;
                    int bulletListsCreatedInFile = 0;
                    int noPageBreakCreatedInFile = 0;

                    bool changed = false;

                    // On cible uniquement les conteneurs qui ont directement des paragraphes de puce.
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

                        // Ligne de détail ajoutée au rapport final
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

        /// <summary>
        ///  Transforme un conteneur htm qui contient directement des paragraphes de type puce.
        ///  Cette méthode reconstruit les enfants du conteneur:
        ///  - les paragraphes normaux sont conservés tels quels
        ///  - les suites de paragraphes a_tiret sont regroupés dans une lsite ul
        ///  - les paragraphes vides entre deux puces sont ignorés
        ///  - une liste courte peut être regroupée avec son paragraphe d'introduction ds un div.a_NOpagebreak
        /// </summary>
        /// <param name="container"></param> Elément parent contenant les paragraphes à transformer
        /// <param name="bulletParagraphsDetected"></param> Compteur de parag à puces détéctés
        /// <param name="bulletListsCreated"></param> Compteur de listes créées
        /// <param name="noPageBreakBlocksCreated"></param> Compteur des blocs a_NOpagebreak créés
        /// <returns></returns> True si le conteneur a été modifié, sinon false
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
                // Ignore les paragraphes vides placés juste avant une liste à puces
                if (IsIgnorableEmptyParagraph(current) && HasNextBulletParagraph(children, index))
                {
                    index++;
                    continue;
                }

                if (IsBulletParagraph(current))
                {
                    XNamespace ns = current.Name.Namespace;

                    List<XElement> bulletParagraphs = new List<XElement>();
                    // Récupère toute la séquence de paragraphes de type puce
                    while (index < children.Count)
                    {
                        XElement nextElement = children[index];
                        // Paragraphes vides au milieu d'une liste sont ignorés
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
                    // Construit une vrai liste htm à partir des paragraphes AIT
                    XElement bulletList = BuildBulletList(ns, bulletParagraphs);

                    bulletParagraphsDetected += bulletParagraphs.Count;
                    bulletListsCreated++;

                    XElement introParagraph = null;
                    // Si la liste est courte et précédée par un paragraphe d'introduction, on regroupe les deux ds un div.a_NOpagebreak
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
                // Si l'élément n'est pas une puce, on le conserve tel quel
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
        /// Construit une liste htm à partir des paragraphes de puces détectés.
        /// Les niveaux sont determinés grâce aux classes AIT:
        /// - niv 1: a_tiret
        /// - niv 2: a_tiret_retrait_2
        /// - niv 3: a_tiret_retrait_3
        /// </summary>
        /// <param name="ns"></param> namespace xml à conserver pour les nouveaux éléments
        /// <param name="bulletParagraphs"></param> paragraphes de puce à transformer
        /// <returns></returns> Liste ul structurée
        private XElement BuildBulletList(XNamespace ns, List<XElement> bulletParagraphs)
        {
            XElement rootList = new XElement(ns + "ul");

            // Garde en mémoire le dernier li rencontré pour chaque niveau, permet d'ajouter les sous-listes au bon parent.
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

                // Niveau 1: ajout d'une sous liste du dernier élément parent
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
        /// <summary>
        /// Récupère la sous liste ul existante d'un li, ou la crée si elle n'existe pas.
        /// Utilisé pour construire les niveaux imbriqués des listes à puces.
        /// </summary>
        /// <param name="listItem"></param> Element li parent
        /// <param name="ns"></param>
        /// <returns></returns> sous liste ul existante ou nouvellement créée
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

        /// <summary>
        /// Supprime de la mémoire les niveaux plus profonds que le niveau courant.
        /// Ex: si on revient d'un niv 3 vers un niv 1, les anciens parents de niveau 2 et 3 ne doivent plus être utilisés
        /// </summary>
        /// <param name="lastListItemByLevel"></param> Dictionnaire des derniers li par niveau
        /// <param name="currentLevel"></param> niveau courant de la liste
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

        /// <summary>
        /// Récupère le paragraphe d'introduction situé juste avant une liste, puis le retire temporairement de la liste des nvx noeuds.
        /// Ca permet de créer le bloc div.a_NOpagebreak contenant le paragraphe d'introduction + la liste
        /// </summary>
        /// <param name="newNodes"></param> Liste des noeuds deja reconstruits
        /// <param name="introParagraph"></param> paragraphe d'introduction trouvé
        /// <returns></returns> True si un paragraphe d'introduction valide a été trouvé, sinon false
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
        /// <summary>
        /// Vérifie si un paragraphe peut être considéré comme une introduction de liste.
        /// Un paragraphe d'introduction doit etre un paragraphe normal (ps une puce, A/R, figure...)
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns> True si le paragraphe peut servir d'introduction à une liste
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

        /// <summary>
        /// Vérifie si un élément contient directement au moins un parag de type puce
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool HasDirectBulletChild(XElement element)
        {
            return element
                .Elements()
                .Any(IsBulletParagraph);
        }
        // Vérifie s'il existe un paragraphe de puce après l'index courant, parag vides ignorés pdnt cette recherche
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

        // Vérifie si un parag correspond à une puce AIT, les classes de puces commencent par a_tiret
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

        // Détermine le niveau de retrait d'une puce (règles énoncées plus haut)
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

        // Vérifie si un parag peut être ignoré entre 2 puces pr éviter de casser la liste
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

        // Vérifie si l'élément XML est un paragraphe <p>
        private bool IsParagraph(XElement element)
        {
            return element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase);
        }

        // Clone un paragraphe et supprime son attribut class
        private XElement CloneParagraphWithoutClass(XElement paragraph)
        {
            XElement clone = new XElement(paragraph);
            clone.Attribute("class")?.Remove();
            return clone;
        }

        // Vérifie si un élément XML possède une classe CSS donnée
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