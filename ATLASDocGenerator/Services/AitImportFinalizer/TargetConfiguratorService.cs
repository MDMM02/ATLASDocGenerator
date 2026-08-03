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
    /// Cette classe configure une target MadCap après l'import AIT
    /// Elle met à jour:
    /// - TOC princiaple utilisée par la target
    /// - feuille de style principale
    /// - le layout de page principal
    /// Ces valeurs viennent du profil associé au type de document selectionné
    /// </summary>
    public class TargetConfiguratorService
    {
        /// <summary>
        ///  Configure une target M dCap avec les ressources du profil sélectionné
        ///  Traitement:
        ///  1. Vérifie que la target, TOC et profil sont valides
        ///  2. Crée une sauvegarde de la target
        ///  3. Charge le fichier target comme document XML
        ///  4. Convertit les chemins au format attendu
        ///  5. Met à jour la TOC principale
        ///  6. Met à jour la feuille de style principale
        ///  7. Met à jour le layout de page principal
        ///  8. Sauvegarde la target modifiée
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="tocPath"></param>
        /// <param name="profile"></param>
        
        public void ConfigureTarget(string targetPath, string tocPath, AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new ArgumentException("Le chemin de la target est vide.", "targetPath");
            }

            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException("Le fichier target est introuvable.", targetPath);
            }

            if (string.IsNullOrWhiteSpace(tocPath))
            {
                throw new ArgumentException("Le chemin de la TOC est vide.", "tocPath");
            }

            if (!File.Exists(tocPath))
            {
                throw new FileNotFoundException("Le fichier TOC est introuvable.", tocPath);
            }

            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            // Charge la target comme document XML en conservant les espaces existants.
            XDocument document = XDocument.Load(targetPath, LoadOptions.PreserveWhitespace);

            if (document.Root == null)
            {
                throw new InvalidOperationException("Le fichier target ne possède pas de racine XML.");
            }

            // Convertit le chemin absolu de la TOC en chemin relatif utilisable par Flare
            string tocValue = ConvertTocPathToFlarePath(tocPath);

            // Les chemins des ressources sont conservés relativement au dossier Content.
            // Exemple :Resources/Stylesheets/Styles.css
            string stylesheetValue = NormalizeContentRelativePath(profile.PrimaryStylesheet);
            string pageLayoutValue = NormalizeContentRelativePath(profile.PrimaryPageLayout);

            // Configure la TOC principale de la target.
            // Vérifier : leur nom peut varier selon le type ou la version de la target Flare.
            SetAttributeValue(document, new[] { "MasterToc", "MasterTOC", "PrimaryToc", "PrimaryTOC", "Toc", "TOC" }, "MasterToc", tocValue);
            // Configure la feuille de style principale de la target
            SetAttributeValue(document,new[] { "MasterStylesheet", "PrimaryStylesheet", "Stylesheet" }, "MasterStylesheet", stylesheetValue);
            //Configure le layout de page principal de la target.
           
           SetAttributeValue(document,new[] { "MasterPageLayout", "PrimaryPageLayout", "PageLayout" }, "MasterPageLayout",pageLayoutValue);

            // Crée la sauvegarde seulement après validation complète du XML,
            // juste avant la première écriture sur disque.
            FileBackupService.CreateInitialBackup(
                targetPath,
                ".bak");

            document.Save(
                targetPath,
                SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Crée une copie de sauvegarde de la target
        /// Le fichier .bak est créé uniquement s'il n'existe pas déjà.
        /// </summary>
        /// <param name="filePath"></param>
        // Convertit le chemin complet d'une TOC en chemin relatif utilisable ds une target MadCap Flare
        private string ConvertTocPathToFlarePath(string tocPath)
        {
            // Uniformise les séparateurs pour utiliser le format avec des slashs
            string normalizedPath = tocPath.Replace("\\", "/");

            int projectIndex = normalizedPath.IndexOf("/Project/", StringComparison.OrdinalIgnoreCase);

            if (projectIndex >= 0)
            {
                // Retire toute la partie précédant le dossier Project
                return normalizedPath.Substring(projectIndex + 1);
            }
            // Fallback utilisé si le chemin ne contient pas explicitement /Project/
            return "Project/TOCs/" + Path.GetFileName(tocPath);
        }

        // Normalise le chemin d'une ressource relativement au dossier Content
        private string NormalizeContentRelativePath(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return string.Empty;
            }

            string normalizedPath = resourcePath
                .Replace("\\", "/")
                .Trim()
                .TrimStart('/');

            // Évite d'obtenir /Content/Content/... si le profil
            // contient déjà le dossier Content.
            if (normalizedPath.StartsWith(
                "Content/",
                StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(
                    "Content/".Length);
            }

            return "/Content/" + normalizedPath;
        }

        // Recherche un attribut dans la target et met à jour sa valeur.
        // Plusieurs noms d'attributs peuvent êtr(e fournis afin de gérer les différentes variantes ds les targets.
        private void SetAttributeValue(
           XDocument document,
           string[] possibleAttributeNames,
           string defaultAttributeName,
           string value)
        {
            XElement root = document.Root;

            if (root == null)
            {
                throw new InvalidOperationException(
                    "Le fichier target ne possède pas de racine XML."
                );
            }

            value = value ?? string.Empty;

            // La racine est prioritaire. Si le réglage n'est présent que dans
            // les descendants, un emplacement unique est exigé afin de ne pas
            // modifier arbitrairement une section secondaire de la target.
            List<XAttribute> rootAttributes = root
                .Attributes()
                .Where(attribute =>
                    possibleAttributeNames.Any(name =>
                        attribute.Name.LocalName.Equals(
                            name,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (rootAttributes.Count > 1)
            {
                throw CreateAmbiguousAttributeException(
                    defaultAttributeName,
                    rootAttributes);
            }

            XAttribute existingAttribute = rootAttributes
                .FirstOrDefault();

            if (existingAttribute == null)
            {
                List<XAttribute> descendantAttributes = root
                    .Descendants()
                    .SelectMany(element => element.Attributes())
                    .Where(attribute =>
                        possibleAttributeNames.Any(name =>
                            attribute.Name.LocalName.Equals(
                                name,
                                StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (descendantAttributes.Count > 1)
                {
                    throw CreateAmbiguousAttributeException(
                        defaultAttributeName,
                        descendantAttributes);
                }

                existingAttribute = descendantAttributes
                    .FirstOrDefault();
            }

            if (existingAttribute != null)
            {
                // Conserve le style du chemin existant,/ notamment la présence éventuelle d'un slash initial.
                existingAttribute.Value = MatchExistingPathStyle(
                    existingAttribute.Value,
                    value
                );

                return;
            }

            // Fallback :
            // si aucun attribut connu n'existe dans la target,
            // il est ajouté directement sur la racine.
            root.SetAttributeValue(
                defaultAttributeName,
                value
            );
        }

        private InvalidOperationException CreateAmbiguousAttributeException(
            string settingName,
            IEnumerable<XAttribute> attributes)
        {
            string locations = string.Join(
                ", ",
                attributes.Select(attribute =>
                    attribute.Parent.Name.LocalName
                    + "@"
                    + attribute.Name.LocalName));

            return new InvalidOperationException(
                "La target contient plusieurs emplacements possibles pour "
                + settingName
                + " : "
                + locations
                + ". Aucun emplacement n'a été modifié.");
        }

        // Adapte le nv chemin auformat de l'ancienne valeur.
        private string MatchExistingPathStyle(string existingValue, string newValue)
        {
            if (!string.IsNullOrWhiteSpace(existingValue)
                && existingValue.StartsWith("/")
                && !newValue.StartsWith("/"))
            {
                return "/" + newValue;
            }

            return newValue;
        }
    }
}
