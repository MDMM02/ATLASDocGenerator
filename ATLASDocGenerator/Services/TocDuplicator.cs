using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Cette classe duplique et configure une TOC MadCap Flare lors de la génération d'un nouveau document.
    ///
    /// Elle utilise en priorité la TOC modèle Project/TOCs/PS_DR_tech.fltoc.
    /// L'ancien nom Project/TOCs/Doc_SAV.fltoc reste accepté pour compatibilité.
    ///
    /// La TOC copiée est ensuite adaptée :
    /// - certaines conditions MadCap sont retirées
    /// - les liens vers les topics modèles sont remplacés
    ///   par les liens vers les topics du nouveau document
    ///
    /// La nouvelle TOC est enregistrée dans Project/TOCs avec le même nom que le dossier documentaire.
    /// </summary>
    public class TocDuplicator
    {
        // Namespace MadCap utilisé pour les attributs MadCap:conditions.
        private static readonly XNamespace MadCapNs =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        /// <summary>
        /// Duplique la TOC modèle puis met à jour son contenu.
        ///
        /// Traitement :
        /// 1. Vérifie que la TOC modèle existe
        /// 2. Vérifie qu'aucune TOC du même nom n'existe déjà
        /// 3. Copie la TOC modèle
        /// 4. Charge la copie comme document XML
        /// 5. Retire certaines conditions MadCap
        /// 6. Remplace les liens vers les topics modèles
        /// 7. Sauvegarde et retourne le chemin de la nouvelle TOC
        /// </summary>
        /// <param name="projectRoot">Chemin racine du projet MadCap Flare.</param>
        /// <param name="folderName">Nom normalisé du dossier documentaire.</param>
        /// <param name="safeReference">Référence normalisée du document.</param>
        /// <returns>Chemin complet de la TOC créée.</returns>
        public string DuplicateAndUpdateToc(
            string projectRoot,
            string folderName,
            string safeReference)
        {
            string targetTocPath = Path.Combine(
                projectRoot,
                "Project",
                "TOCs",
                folderName + ".fltoc"
            );

            if (File.Exists(targetTocPath))
            {
                throw new Exception(
                    "Une TOC existe déjà avec ce nom :\n"
                    + targetTocPath
                );
            }

            string sourceDescription;
            XDocument document = LoadSourceToc(
                projectRoot,
                out sourceDescription
            );

            if (document.Root == null)
            {
                throw new Exception(
                    "La TOC ne contient pas d'élément racine."
                );
            }

            // Retire les conditions qui ne doivent plus être présentes directement dans les entrées de la TOC.
            RemoveTocConditions(document);

            // Remplace les liens des topics modèles par les liens des topics du nouveau document.
            UpdateTocLinks(
                document,
                folderName,
                safeReference
            );

            // Sauvegarde sans reformater inutilement le XML.
            document.Save(
                targetTocPath,
                SaveOptions.DisableFormatting
            );

            return targetTocPath;
        }

        /// <summary>
        /// Charge la TOC du projet lorsqu'elle existe, sinon le modèle embarqué
        /// dans la DLL. Le modèle du projet reste prioritaire afin de permettre
        /// une mise à jour locale contrôlée.
        /// </summary>
        internal XDocument LoadSourceToc(
            string projectRoot,
            out string sourceDescription)
        {
            string tocFolder = Path.Combine(
                projectRoot,
                "Project",
                "TOCs"
            );

            string[] candidates =
            {
                Path.Combine(tocFolder, "PS_DR_tech.fltoc"),
                Path.Combine(tocFolder, "Doc_SAV.fltoc")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    sourceDescription = candidate;

                    try
                    {
                        return XDocument.Load(
                            candidate,
                            LoadOptions.PreserveWhitespace
                        );
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException(
                            "Impossible de lire la TOC modèle comme XML :\n"
                            + candidate
                            + "\n\nDétail : "
                            + ex.Message,
                            ex
                        );
                    }
                }
            }

            sourceDescription = "TOC PS embarquée dans ATLASDocGenerator.dll";

            try
            {
                return XDocument.Parse(
                    EmbeddedDocGeneratorTemplates.GetPsTocXml(),
                    LoadOptions.PreserveWhitespace
                );
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    "Impossible de lire la TOC PS embarquée dans la DLL.",
                    ex
                );
            }
        }

        /// <summary>
        /// Retire certaines conditions des éléments de la TOC.
        ///
        /// La méthode traite :
        /// - les attributs conditions sans namespace
        /// - les attributs MadCap:conditions
        ///
        /// Les autres conditions éventuellement présentes sont conservées.
        /// </summary>
        /// <param name="document">Document XML représentant la TOC.</param>
        private void RemoveTocConditions(XDocument document)
        {
            if (document.Root == null)
            {
                return;
            }

            // Parcourt la racine puis tous ses descendants.
            // Cette écriture évite l'utilisation de DescendantsAndSelf(), qui avait déjà causé un problème de compatibilité.
            IEnumerable<XElement> elements = new[] { document.Root }
                .Concat(document.Root.Descendants());

            foreach (XElement element in elements)
            {
                RemoveConditionsFromAttribute(
                    element,
                    "conditions"
                );

                RemoveConditionsFromAttribute(
                    element,
                    MadCapNs + "conditions"
                );
            }
        }

        /// <summary>
        /// Retire les conditions ciblées d'un attribut XML.
        ///
        /// Si d'autres conditions sont présentes dans le même attribut, elles sont conservées.
        ///
        /// Si aucune condition ne reste après le nettoyage, l'attribut est complètement supprimé.
        /// </summary>
        /// <param name="element">Élément XML à nettoyer.</param>
        /// <param name="attributeName">Nom de l'attribut conditions.</param>
        private void RemoveConditionsFromAttribute(
            XElement element,
            XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return;
            }

            string[] conditionsToRemove =
            {
                "Stago_Gestion.Contenu commun",
                "Stago_Gestion.40_DoNotTranslate",
                "Stago_Gestion.Commun_Tech"
            };

            string[] remainingConditions = attribute.Value
                .Split(',')
                .Select(condition => condition.Trim())
                .Where(condition =>
                    !conditionsToRemove.Any(conditionToRemove =>
                        condition.Equals(
                            conditionToRemove,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(condition =>
                    !string.IsNullOrWhiteSpace(condition)
                )
                .ToArray();

            if (remainingConditions.Length == 0)
            {
                attribute.Remove();
            }
            else
            {
                attribute.Value = string.Join(
                    ",",
                    remainingConditions
                );
            }
        }

        /// <summary>
        /// Met à jour les liens des entrées de la TOC.
        ///
        /// Chaque lien vers un topic modèle est comparé à la table de remplacement créée par BuildLinkReplacementMap.
        ///
        /// Si une correspondance est trouvée, le lien est remplacé par le chemin du topic créé dans le nouveau dossier documentaire.
        /// </summary>
        /// <param name="document">Document XML représentant la TOC.</param>
        /// <param name="folderName">Nom du dossier documentaire.</param>
        /// <param name="safeReference">Référence normalisée du document.</param>
        private void UpdateTocLinks(
            XDocument document,
            string folderName,
            string safeReference)
        {
            Dictionary<string, string> replacements =
                BuildLinkReplacementMap(
                    folderName,
                    safeReference
                );

            foreach (XElement element in document.Descendants())
            {
                // Recherche l'attribut Link sans dépendre des majuscules ou d'un éventuel namespace XML.
                XAttribute linkAttribute = element
                    .Attributes()
                    .FirstOrDefault(attribute =>
                        attribute.Name.LocalName.Equals(
                            "Link",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (linkAttribute == null)
                {
                    continue;
                }

                // Uniformise les slashs avant de rechercher le lien.
                string oldLink = NormalizeFlarePath(
                    linkAttribute.Value
                );

                string newLink;

                if (replacements.TryGetValue(oldLink, out newLink))
                {
                    linkAttribute.Value = newLink;
                }
            }
        }

        /// <summary>
        /// Crée la table de correspondance entre les topics modèles et les topics du nouveau document.
        ///
        /// La clé correspond au lien présent dans la TOC technique modèle.
        /// La valeur correspond au lien du topic généré.
        /// </summary>
        /// <param name="folderName">Nom du nouveau dossier documentaire.</param>
        /// <param name="safeReference">Référence normalisée du document.</param>
        /// <returns>Table des liens à remplacer.</returns>
        private Dictionary<string, string> BuildLinkReplacementMap(
            string folderName,
            string safeReference)
        {
            // La comparaison ne tient pas compte des majuscules.
            Dictionary<string, string> map =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

            map.Add(
                "/Content/Template_tech/Title_doc.htm",
                "/Content/"
                + folderName
                + "/Title_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Resources/Commun Stago/topics_Tech/Historique_tech.htm",
                "/Content/"
                + folderName
                + "/Historique_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Objectif.htm",
                "/Content/"
                + folderName
                + "/Objectif_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Mesures de sécurité.htm",
                "/Content/"
                + folderName
                + "/Mesures_securite_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Matériel nécessaire.htm",
                "/Content/"
                + folderName
                + "/Materiel_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Documents nécessaires.htm",
                "/Content/"
                + folderName
                + "/Documents_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Duree_inter_Remplacements.htm",
                "/Content/"
                + folderName
                + "/Duree_inter_Remplacements_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/Prérequis.htm",
                "/Content/"
                + folderName
                + "/Prerequis_"
                + safeReference
                + ".htm"
            );

            map.Add(
                "/Content/Template_tech/1er_chapitre.htm",
                "/Content/"
                + folderName
                + "/1er_chapitre.htm"
            );

            return map;
        }

        /// <summary>
        /// Normalise un chemin utilisé dans une TOC MadCap Flare.
        ///
        /// La méthode :
        /// - remplace les antislashs par des slashs
        /// - retire les espaces au début et à la fin
        /// - ajoute un slash initial s'il est absent
        ///
        /// Cette normalisation permet de comparer plus facilement les liens du fichier TOC avec la table de remplacement.
        /// </summary>
        /// <param name="path">Chemin à normaliser.</param>
        /// <returns>Chemin normalisé.</returns>
        private string NormalizeFlarePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalizedPath = path
                .Replace("\\", "/")
                .Trim();

            if (!normalizedPath.StartsWith("/"))
            {
                normalizedPath = "/" + normalizedPath;
            }

            return normalizedPath;
        }
    }
}
