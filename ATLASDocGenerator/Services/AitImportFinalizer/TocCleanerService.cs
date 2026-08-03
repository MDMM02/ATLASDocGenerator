using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    /// <summary>
    /// Cette classe nettoie une TOC MadCap Flare après un import Author-it.
    ///
    /// Elle recherche les entrées parasites définies dans le profil du document,
    /// par exemple :
    /// - A_HEADER
    /// - A_FOOTER
    /// - Table des matières
    /// - Cover
    ///
    /// La recherche est effectuée dans les attributs Title, Link et Source
    /// de chaque élément TocEntry.
    ///
    /// Les entrées correspondantes sont supprimées de la TOC
    /// et leur nom est retourné pour alimenter le rapport du Finalizer.
    /// </summary>
    public class TocCleanerService
    {
        /// <summary>
        /// Nettoie la TOC sélectionnée selon les règles définies
        /// dans le profil du type de document.
        ///
        /// Traitement :
        /// 1. Vérifie que le fichier TOC et le profil sont valides
        /// 2. Charge le fichier TOC comme document XML
        /// 3. Recherche les entrées correspondant aux motifs du profil
        /// 4. Crée une sauvegarde avant la première modification
        /// 5. Supprime les entrées parasites
        /// 6. Sauvegarde la TOC si elle a été modifiée
        /// 7. Retourne la liste des entrées supprimées
        /// </summary>
        /// <param name="tocPath">Chemin complet du fichier TOC .fltoc.</param>
        /// <param name="profile">Profil correspondant au type de document sélectionné.</param>
        /// <returns>Liste des entrées supprimées de la TOC.</returns>
        public List<string> CleanToc(
            string tocPath,
            AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(tocPath))
            {
                throw new ArgumentException(
                    "Le chemin de la TOC est vide.",
                    "tocPath"
                );
            }

            if (!File.Exists(tocPath))
            {
                throw new FileNotFoundException(
                    "Le fichier TOC est introuvable.",
                    tocPath
                );
            }

            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            if (profile.TocEntriesToRemove == null)
            {
                throw new InvalidOperationException(
                    "La liste des entrées à supprimer n'est pas définie dans le profil."
                );
            }

            // Charge la TOC comme document XML en conservant les espaces existants.
            XDocument document = XDocument.Load(
                tocPath,
                LoadOptions.PreserveWhitespace
            );

            if (document.Root == null)
            {
                throw new InvalidOperationException(
                    "Le fichier TOC ne possède pas de racine XML."
                );
            }

            // Recherche toutes les entrées TocEntry correspondant aux motifs définis dans le profil.
            //
            // ToList est important ici :
            // il permet de créer une copie de la sélection avant de commencer à supprimer des éléments du document.
            List<XElement> entriesToRemove = document
                .Descendants()
                .Where(element =>
                    element.Name.LocalName.Equals(
                        "TocEntry",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Where(element =>
                    ShouldRemoveEntry(
                        element,
                        profile.TocEntriesToRemove
                    )
                )
                .ToList();

            List<string> removedEntries = new List<string>();

            if (entriesToRemove.Count == 0)
            {
                return removedEntries;
            }

            // Crée une sauvegarde avant la première modification de la TOC.
            FileBackupService.CreateInitialBackup(
                tocPath,
                ".bak");

            foreach (XElement entry in entriesToRemove)
            {
                // Récupère un libellé lisible pour le rapport.
                string label = GetEntryLabel(entry);
                removedEntries.Add(label);

                // Supprime l'entrée de la TOC.
                //
                // Attention : si cette entrée contient des sous-entrées, elles seront également supprimées avec leur parent.
                entry.Remove();
            }

            // Sauvegarde la TOC sans reformater inutilement tout le XML.
            document.Save(
                tocPath,
                SaveOptions.DisableFormatting
            );

            return removedEntries;
        }

        /// <summary>
        /// Vérifie si une entrée de TOC doit être supprimée.
        ///
        /// La recherche est effectuée dans les attributs :
        /// - Title
        /// - Link
        /// - Source
        ///
        /// Une entrée est supprimée si l'une de ces valeurs contient un motif défini dans le profil.
        /// La comparaison ne tient pas compte des majuscules.
        /// </summary>
        /// <param name="entry">Entrée TocEntry à analyser.</param>
        /// <param name="patterns">Motifs définis dans le profil.</param>
        /// <returns>True si l'entrée doit être supprimée, sinon false.</returns>
        private bool ShouldRemoveEntry(
            XElement entry,
            List<string> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return false;
            }

            string title = GetAttributeValue(entry, "Title");
            string link = GetAttributeValue(entry, "Link");
            string source = GetAttributeValue(entry, "Source");

            string combined = title + " " + link + " " + source;

            foreach (string pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                if (combined.IndexOf(
                    pattern.Trim(),
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retourne un libellé lisible pour une entrée supprimée.
        ///
        /// Priorité :
        /// 1. Valeur de l'attribut Title
        /// 2. Valeur de l'attribut Link
        /// 3. Texte générique si aucun des deux n'est disponible
        /// </summary>
        /// <param name="entry">Entrée TocEntry supprimée.</param>
        /// <returns>Libellé à ajouter dans le rapport.</returns>
        private string GetEntryLabel(XElement entry)
        {
            string title = GetAttributeValue(entry, "Title");

            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            string link = GetAttributeValue(entry, "Link");

            if (!string.IsNullOrWhiteSpace(link))
            {
                return link;
            }

            return "(entrée TOC sans nom)";
        }

        /// <summary>
        /// Récupère la valeur d'un attribut XML.
        ///
        /// La recherche ne tient pas compte des majuscules et utilise LocalName pour éviter les problèmes de namespace.
        /// Retourne une chaîne vide si l'attribut n'existe pas.
        /// </summary>
        /// <param name="element">Élément XML à analyser.</param>
        /// <param name="attributeName">Nom de l'attribut recherché.</param>
        /// <returns>Valeur de l'attribut ou chaîne vide.</returns>
        private string GetAttributeValue(
            XElement element,
            string attributeName)
        {
            XAttribute attribute = element
                .Attributes()
                .FirstOrDefault(candidate =>
                    candidate.Name.LocalName.Equals(
                        attributeName,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (attribute == null)
            {
                return string.Empty;
            }

            return attribute.Value ?? string.Empty;
        }
    }
}
