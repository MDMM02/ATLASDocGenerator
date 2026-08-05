using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Cette classe applique un post-traitement aux topics après leur duplication dans le nouveau dossier documentaire.
    ///
    /// Elle réalise principalement deux opérations :
    /// - suppression de la condition Stago_Gestion.Contenu commun
    /// - mise à jour des chemins vers les ressources
    ///
    /// Les chemins présents dans les attributs src, href et style sont recalculés afin de rester valides depuis le dossier dans lequel le topic a été copié.
    ///
    /// Cette classe ne modifie pas le topic modèle d'origine.
    /// Elle intervient uniquement sur la copie.
    /// </summary>
    public class TopicPostProcessor
    {
        // Namespace MadCap utilisé pour repérer
        // les attributs MadCap:conditions.
        private static readonly XNamespace MadCapNs =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        /// <summary>
        /// Applique le post-traitement à un topic copié.
        ///
        /// Traitement :
        /// 1. Vérifie que le topic source existe
        /// 2. Vérifie que le topic copié existe
        /// 3. Charge le topic copié comme document XML/XHTML
        /// 4. Retire la condition Contenu commun
        /// 5. Met à jour les liens vers les ressources
        /// 6. Sauvegarde le topic modifié
        /// </summary>
        /// <param name="sourceTopicPath">
        /// Chemin complet du topic modèle d'origine.
        /// </param>
        /// <param name="copiedTopicPath">
        /// Chemin complet du topic copié dans le nouveau dossier documentaire.
        /// </param>
        public void ProcessCopiedTopic(
            string sourceTopicPath,
            string copiedTopicPath)
        {
            if (string.IsNullOrWhiteSpace(sourceTopicPath))
            {
                throw new ArgumentException(
                    "Le chemin du topic source est vide.",
                    "sourceTopicPath"
                );
            }

            if (string.IsNullOrWhiteSpace(copiedTopicPath))
            {
                throw new ArgumentException(
                    "Le chemin du topic copié est vide.",
                    "copiedTopicPath"
                );
            }

            if (!File.Exists(sourceTopicPath))
            {
                throw new Exception(
                    "Topic source introuvable :\n"
                    + sourceTopicPath
                );
            }

            if (!File.Exists(copiedTopicPath))
            {
                throw new Exception(
                    "Topic copié introuvable :\n"
                    + copiedTopicPath
                );
            }

            XDocument document;

            try
            {
                // Charge le topic copié en conservant les espaces et retours à la ligne existants.
                document = XDocument.Load(
                    copiedTopicPath,
                    LoadOptions.PreserveWhitespace
                );
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Impossible de lire le topic copié comme XML/XHTML :\n"
                    + copiedTopicPath
                    + "\n\nDétail : "
                    + ex.Message
                );
            }

            if (document.Root == null)
            {
                throw new InvalidOperationException(
                    "Le topic copié ne possède pas de racine XML."
                );
            }

            // Un document dupliqué doit être autonome : retire toutes les conditions.
            RemoveCommonContentCondition(document);

            // Recalcule les chemins des ressources par rapport au nouveau dossier du topic.
            UpdateResourceLinks(
                document,
                sourceTopicPath,
                copiedTopicPath
            );

            // Sauvegarde sans reformater inutilement  l'ensemble du fichier XHTML.
            document.Save(
                copiedTopicPath,
                SaveOptions.DisableFormatting
            );
        }

        /// <summary>
        /// Retire la condition Contenu commun de tous les éléments du topic.
        ///
        /// La méthode traite :
        /// - les attributs MadCap:conditions
        /// - les attributs conditions sans namespace
        ///
        /// Les autres conditions éventuellement présentes dans le même attribut sont conservées.
        /// </summary>
        /// <param name="document">
        /// Document XML représentant le topic copié.
        /// </param>
        private void RemoveCommonContentCondition(XDocument document)
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
                XAttribute madCapConditions = element.Attribute(MadCapNs + "conditions");
                if (madCapConditions != null)
                    madCapConditions.Remove();

                XAttribute plainConditions = element.Attribute("conditions");
                if (plainConditions != null)
                    plainConditions.Remove();
            }
        }

        /// <summary>
        /// Retire la condition Contenu commun d'un attribut conditions.
        ///
        /// Les deux variantes suivantes sont reconnues :
        /// - Stago_Gestion.Contenu commun
        /// - Contenu commun
        ///
        /// Si aucune autre condition ne reste, l'attribut est supprimé complètement.
        /// </summary>
        /// <param name="element">Élément XML à nettoyer.</param>
        /// <param name="attributeName">
        /// Nom de l'attribut conditions à analyser.
        /// </param>
        private void RemoveConditionFromAttribute(
            XElement element,
            XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return;
            }

            string[] remainingConditions = attribute.Value
                .Split(',')
                .Select(condition => condition.Trim())
                .Where(condition =>
                    !string.Equals(
                        condition,
                        "Stago_Gestion.Contenu commun",
                        StringComparison.OrdinalIgnoreCase
                    )
                    && !string.Equals(
                        condition,
                        "Contenu commun",
                        StringComparison.OrdinalIgnoreCase
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
        /// Met à jour les chemins des ressources présentes dans le topic.
        ///
        /// La méthode analyse les attributs :
        /// - src
        /// - href
        /// - style
        ///
        /// Les chemins relatifs sont recalculés pour être valides depuis le nouveau dossier documentaire.
        /// </summary>
        /// <param name="document">Topic copié chargé comme document XML.</param>
        /// <param name="sourceTopicPath">Chemin du topic modèle.</param>
        /// <param name="copiedTopicPath">Chemin du topic copié.</param>
        private void UpdateResourceLinks(
            XDocument document,
            string sourceTopicPath,
            string copiedTopicPath)
        {
            string sourceTopicFolder =
                Path.GetDirectoryName(sourceTopicPath);

            string copiedTopicFolder =
                Path.GetDirectoryName(copiedTopicPath);

            if (string.IsNullOrWhiteSpace(sourceTopicFolder))
            {
                throw new DirectoryNotFoundException(
                    "Impossible de déterminer le dossier du topic source : "
                    + sourceTopicPath
                );
            }

            if (string.IsNullOrWhiteSpace(copiedTopicFolder))
            {
                throw new DirectoryNotFoundException(
                    "Impossible de déterminer le dossier du topic copié : "
                    + copiedTopicPath
                );
            }

            DirectoryInfo contentDirectory =
                Directory.GetParent(copiedTopicFolder);

            if (contentDirectory == null)
            {
                throw new DirectoryNotFoundException(
                    "Impossible de déterminer le dossier Content à partir de : "
                    + copiedTopicFolder
                );
            }

            // Les topics générés sont placés dans :
            // Content/NomDuDocument
            //
            // Le parent du dossier documentaire correspond donc à Content.
            string contentRoot = contentDirectory.FullName;

            string resourcesRoot = Path.Combine(
                contentRoot,
                "Resources"
            );

            if (document.Root == null)
            {
                return;
            }

            IEnumerable<XElement> elements = new[] { document.Root }
                .Concat(document.Root.Descendants());

            foreach (XElement element in elements)
            {
                // ToList permet de modifier les valeurs des attributs sans perturber l'énumération en cours.
                foreach (XAttribute attribute in element
                    .Attributes()
                    .ToList())
                {
                    string localName = attribute.Name.LocalName;

                    if (localName.Equals(
                            "src",
                            StringComparison.OrdinalIgnoreCase)
                        || localName.Equals(
                            "href",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Value = ResolveAndNormalizeReference(
                            attribute.Value,
                            sourceTopicFolder,
                            copiedTopicFolder,
                            resourcesRoot
                        );
                    }

                    if (localName.Equals(
                        "style",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Value = UpdateUrlsInsideStyle(
                            attribute.Value,
                            sourceTopicFolder,
                            copiedTopicFolder,
                            resourcesRoot
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Met à jour les chemins présents dans les fonctions CSS url().
        ///
        /// Exemple :
        /// background-image: url('../Resources/Images/image.png')
        ///
        /// Le chemin contenu dans url() est recalculé comme les attributs src et href.
        /// </summary>
        /// <param name="styleValue">Contenu complet de l'attribut style.</param>
        /// <param name="sourceTopicFolder">Dossier du topic modèle.</param>
        /// <param name="copiedTopicFolder">Dossier du topic copié.</param>
        /// <param name="resourcesRoot">Dossier Content/Resources.</param>
        /// <returns>Style contenant les chemins recalculés.</returns>
        private string UpdateUrlsInsideStyle(
            string styleValue,
            string sourceTopicFolder,
            string copiedTopicFolder,
            string resourcesRoot)
        {
            if (string.IsNullOrWhiteSpace(styleValue))
            {
                return styleValue;
            }

            return Regex.Replace(
                styleValue,
                @"url\((['""]?)([^'"")]+)\1\)",
                match =>
                {
                    string quote = match.Groups[1].Value;
                    string oldPath = match.Groups[2].Value;

                    string newPath = ResolveAndNormalizeReference(
                        oldPath,
                        sourceTopicFolder,
                        copiedTopicFolder,
                        resourcesRoot
                    );

                    return "url("
                        + quote
                        + newPath
                        + quote
                        + ")";
                },
                RegexOptions.IgnoreCase
            );
        }

        /// <summary>
        /// Résout une référence locale puis la convertit en chemin relatif depuis le topic copié.
        ///
        /// La méthode :
        /// - ignore les références externes ou spéciales
        /// - nettoie les répétitions Resources/Resources
        /// - recherche le fichier depuis plusieurs emplacements possibles
        /// - calcule un nouveau chemin relatif
        /// - convertit les antislashs en slashs pour Flare
        /// </summary>
        /// <param name="originalReference">Référence présente dans le topic.</param>
        /// <param name="sourceTopicFolder">Dossier du topic source.</param>
        /// <param name="copiedTopicFolder">Dossier du topic copié.</param>
        /// <param name="resourcesRoot">Dossier Content/Resources.</param>
        /// <returns>Référence normalisée pour le topic copié.</returns>
        private string ResolveAndNormalizeReference(
            string originalReference,
            string sourceTopicFolder,
            string copiedTopicFolder,
            string resourcesRoot)
        {
            if (string.IsNullOrWhiteSpace(originalReference))
            {
                return originalReference;
            }

            // Les liens externes, ancres et références spéciales ne doivent pas être modifiés.
            if (IsExternalOrSpecialReference(originalReference))
            {
                return originalReference;
            }

            // Corrige les chemins du type Resources/Resources.
            string cleanedReference =
                CleanDuplicatedResources(originalReference);

            string absolutePath = TryResolveReference(
                cleanedReference,
                sourceTopicFolder,
                copiedTopicFolder,
                resourcesRoot
            );

            if (absolutePath == null)
            {
                // Si le fichier n'est pas retrouvé, on conserve le chemin nettoyé.
                //
                // Cela évite au minimum de laisser une répétition Resources/Resources.
                return NormalizeForFlarePath(
                    cleanedReference
                );
            }

            string newRelativePath = MakeRelativePath(
                copiedTopicFolder,
                absolutePath
            );

            return NormalizeForFlarePath(
                newRelativePath
            );
        }

        /// <summary>
        /// Tente de retrouver le fichier référencé depuis plusieurs emplacements possibles.
        ///
        /// Ordre de recherche :
        /// 1. dossier du topic modèle
        /// 2. dossier du topic copié
        /// 3. dossier Content/Resources
        /// </summary>
        /// <param name="reference">Référence locale à résoudre.</param>
        /// <param name="sourceTopicFolder">Dossier du topic modèle.</param>
        /// <param name="copiedTopicFolder">Dossier du topic copié.</param>
        /// <param name="resourcesRoot">Dossier Content/Resources.</param>
        /// <returns>
        /// Chemin absolu du fichier trouvé, ou null si aucun fichier n'est trouvé.
        /// </returns>
        private string TryResolveReference(
            string reference,
            string sourceTopicFolder,
            string copiedTopicFolder,
            string resourcesRoot)
        {
            string windowsReference = reference
                .Replace("/", "\\");

            try
            {
                // 1. Recherche depuis le dossier du topic modèle.
                string candidateFromSource = Path.GetFullPath(
                    Path.Combine(
                        sourceTopicFolder,
                        windowsReference
                    )
                );

                if (File.Exists(candidateFromSource))
                {
                    return candidateFromSource;
                }

                // 2. Recherche depuis le dossier du topic copié.
                string candidateFromCopiedTopic = Path.GetFullPath(
                    Path.Combine(
                        copiedTopicFolder,
                        windowsReference
                    )
                );

                if (File.Exists(candidateFromCopiedTopic))
                {
                    return candidateFromCopiedTopic;
                }

                // 3. Recherche dans Content/Resources en utilisant la partie située après "Resources".
                string suffixAfterResources =
                    ExtractSuffixAfterResources(reference);

                if (!string.IsNullOrWhiteSpace(suffixAfterResources))
                {
                    string candidateFromResources = Path.GetFullPath(
                        Path.Combine(
                            resourcesRoot,
                            suffixAfterResources.Replace("/", "\\")
                        )
                    );

                    if (File.Exists(candidateFromResources))
                    {
                        return candidateFromResources;
                    }
                }
            }
            catch (Exception ex)
                when (ex is ArgumentException
                    || ex is NotSupportedException
                    || ex is PathTooLongException)
            {
                // La référence ne correspond pas à un chemin local valide. Elle sera conservée sous sa forme nettoyée.
                return null;
            }

            return null;
        }

        /// <summary>
        /// Nettoie les répétitions incorrectes du dossier Resources.
        ///
        /// Exemple :
        /// ../Resources/Resources/Images/image.png
        /// devient :
        /// ../Resources/Images/image.png
        /// </summary>
        /// <param name="reference">Chemin à nettoyer.</param>
        /// <returns>Chemin sans répétition Resources/Resources.</returns>
        private string CleanDuplicatedResources(string reference)
        {
            string cleaned = reference
                .Replace("\\", "/");

            while (cleaned.Contains("/Resources/Resources/"))
            {
                cleaned = cleaned.Replace(
                    "/Resources/Resources/",
                    "/Resources/"
                );
            }

            while (cleaned.Contains("../Resources/Resources/"))
            {
                cleaned = cleaned.Replace(
                    "../Resources/Resources/",
                    "../Resources/"
                );
            }

            return cleaned;
        }

        /// <summary>
        /// Récupère la partie du chemin située après le dossier Resources.
        ///
        /// Exemple :
        /// ../Resources/Images/image.png
        /// retourne :
        /// Images/image.png
        /// </summary>
        /// <param name="reference">Chemin à analyser.</param>
        /// <returns>
        /// Partie située après Resources, ou null si Resources n'est pas trouvé.
        /// </returns>
        private string ExtractSuffixAfterResources(string reference)
        {
            string normalized = CleanDuplicatedResources(reference)
                .Replace("\\", "/");

            int index = normalized.IndexOf(
                "/Resources/",
                StringComparison.OrdinalIgnoreCase
            );

            if (index >= 0)
            {
                return normalized.Substring(
                    index + "/Resources/".Length
                );
            }

            if (normalized.StartsWith(
                "Resources/",
                StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(
                    "Resources/".Length
                );
            }

            if (normalized.StartsWith(
                "../Resources/",
                StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(
                    "../Resources/".Length
                );
            }

            return null;
        }

        /// <summary>
        /// Vérifie si une référence est externe ou correspond à un type de lien qui ne doit pas être recalculé.
        /// </summary>
        /// <param name="reference">Référence à analyser.</param>
        /// <returns>
        /// True si la référence doit être conservée telle quelle.
        /// </returns>
        private bool IsExternalOrSpecialReference(string reference)
        {
            string value = reference.Trim();

            return value.StartsWith(
                       "http://",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(
                       "https://",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(
                       "mailto:",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(
                       "tel:",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(
                       "data:",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(
                       "javascript:",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(
                       "#",
                       StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("//");
        }

        /// <summary>
        /// Calcule le chemin relatif d'un fichier depuis le dossier du topic copié.
        /// </summary>
        /// <param name="fromFolder">Dossier de départ.</param>
        /// <param name="toFilePath">Chemin absolu du fichier cible.</param>
        /// <returns>Chemin relatif entre les deux emplacements.</returns>
        private string MakeRelativePath(
            string fromFolder,
            string toFilePath)
        {
            Uri fromUri = new Uri(
                AppendDirectorySeparatorChar(fromFolder)
            );

            Uri toUri = new Uri(toFilePath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);

            return Uri.UnescapeDataString(
                relativeUri.ToString()
            );
        }

        /// <summary>
        /// Ajoute un séparateur de dossier à la fin du chemin s'il n'est pas déjà présent.
        ///
        /// Cette opération est nécessaire pour calculer correctement un chemin relatif avec la classe Uri.
        /// </summary>
        /// <param name="path">Chemin de dossier.</param>
        /// <returns>Chemin terminé par un séparateur.</returns>
        private string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        /// <summary>
        /// Convertit les séparateurs Windows en slashs afin d'obtenir un chemin compatible avec MadCap Flare.
        /// </summary>
        /// <param name="path">Chemin à normaliser.</param>
        /// <returns>Chemin utilisant des slashs.</returns>
        private string NormalizeForFlarePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.Replace("\\", "/");
        }
    }
}
