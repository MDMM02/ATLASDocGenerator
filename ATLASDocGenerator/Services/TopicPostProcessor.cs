using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services
{
    public class TopicPostProcessor
    {
        private static readonly XNamespace MadCapNs =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        public void ProcessCopiedTopic(string sourceTopicPath, string copiedTopicPath)
        {
            if (!File.Exists(sourceTopicPath))
                throw new Exception("Topic source introuvable :\n" + sourceTopicPath);

            if (!File.Exists(copiedTopicPath))
                throw new Exception("Topic copié introuvable :\n" + copiedTopicPath);

            XDocument document;

            try
            {
                document = XDocument.Load(copiedTopicPath, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Impossible de lire le topic copié comme XML/XHTML :\n" +
                    copiedTopicPath + "\n\n" +
                    "Détail : " + ex.Message
                );
            }

            RemoveCommonContentCondition(document);
            UpdateResourceLinks(document, sourceTopicPath, copiedTopicPath);

            document.Save(copiedTopicPath);
        }

        private void RemoveCommonContentCondition(XDocument document)
        {
            if (document.Root == null)
                return;

            foreach (XElement element in document.Root.DescendantsAndSelf())
            {
                RemoveConditionFromAttribute(element, MadCapNs + "conditions");
                RemoveConditionFromAttribute(element, "conditions");
            }
        }

        private void RemoveConditionFromAttribute(XElement element, XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
                return;

            string[] remainingConditions = attribute.Value
                .Split(',')
                .Select(condition => condition.Trim())
                .Where(condition =>
                    !string.Equals(condition, "Stago_Gestion.Contenu commun", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(condition, "Contenu commun", StringComparison.OrdinalIgnoreCase))
                .Where(condition => !string.IsNullOrWhiteSpace(condition))
                .ToArray();

            if (remainingConditions.Length == 0)
                attribute.Remove();
            else
                attribute.Value = string.Join(",", remainingConditions);
        }

        private void UpdateResourceLinks(XDocument document, string sourceTopicPath, string copiedTopicPath)
        {
            string sourceTopicFolder = Path.GetDirectoryName(sourceTopicPath);
            string copiedTopicFolder = Path.GetDirectoryName(copiedTopicPath);

            // Les topics générés sont dans Content\<NomDoc>.
            // Donc le parent du dossier généré = Content.
            string contentRoot = Directory.GetParent(copiedTopicFolder).FullName;
            string resourcesRoot = Path.Combine(contentRoot, "Resources");

            foreach (XElement element in document.Descendants())
            {
                foreach (XAttribute attribute in element.Attributes().ToList())
                {
                    string localName = attribute.Name.LocalName;

                    if (localName == "src" || localName == "href")
                    {
                        attribute.Value = ResolveAndNormalizeReference(
                            attribute.Value,
                            sourceTopicFolder,
                            copiedTopicFolder,
                            resourcesRoot
                        );
                    }

                    if (localName == "style")
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

        private string UpdateUrlsInsideStyle(
            string styleValue,
            string sourceTopicFolder,
            string copiedTopicFolder,
            string resourcesRoot)
        {
            if (string.IsNullOrWhiteSpace(styleValue))
                return styleValue;

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

                    return "url(" + quote + newPath + quote + ")";
                }
            );
        }

        private string ResolveAndNormalizeReference(
            string originalReference,
            string sourceTopicFolder,
            string copiedTopicFolder,
            string resourcesRoot)
        {
            if (string.IsNullOrWhiteSpace(originalReference))
                return originalReference;

            if (IsExternalOrSpecialReference(originalReference))
                return originalReference;

            string cleanedReference = CleanDuplicatedResources(originalReference);

            string absolutePath = TryResolveReference(
                cleanedReference,
                sourceTopicFolder,
                copiedTopicFolder,
                resourcesRoot
            );

            if (absolutePath == null)
            {
                // Si on ne trouve pas le fichier, on renvoie au moins le chemin nettoyé.
                // Ça évite de garder Resources/Resources.
                return NormalizeForFlarePath(cleanedReference);
            }

            string newRelativePath = MakeRelativePath(copiedTopicFolder, absolutePath);
            return NormalizeForFlarePath(newRelativePath);
        }

        private string TryResolveReference(
            string reference,
            string sourceTopicFolder,
            string copiedTopicFolder,
            string resourcesRoot)
        {
            string windowsReference = reference.Replace("/", "\\");

            // 1. Essayer depuis le dossier source du template.
            string candidateFromSource = Path.GetFullPath(Path.Combine(sourceTopicFolder, windowsReference));
            if (File.Exists(candidateFromSource))
                return candidateFromSource;

            // 2. Essayer depuis le dossier du topic copié.
            string candidateFromCopiedTopic = Path.GetFullPath(Path.Combine(copiedTopicFolder, windowsReference));
            if (File.Exists(candidateFromCopiedTopic))
                return candidateFromCopiedTopic;

            // 3. Essayer depuis Content\Resources à partir du suffixe après "Resources".
            string suffixAfterResources = ExtractSuffixAfterResources(reference);

            if (!string.IsNullOrWhiteSpace(suffixAfterResources))
            {
                string candidateFromResources = Path.GetFullPath(
                    Path.Combine(resourcesRoot, suffixAfterResources.Replace("/", "\\"))
                );

                if (File.Exists(candidateFromResources))
                    return candidateFromResources;
            }

            return null;
        }

        private string CleanDuplicatedResources(string reference)
        {
            string cleaned = reference.Replace("\\", "/");

            while (cleaned.Contains("/Resources/Resources/"))
                cleaned = cleaned.Replace("/Resources/Resources/", "/Resources/");

            while (cleaned.Contains("../Resources/Resources/"))
                cleaned = cleaned.Replace("../Resources/Resources/", "../Resources/");

            return cleaned;
        }

        private string ExtractSuffixAfterResources(string reference)
        {
            string normalized = CleanDuplicatedResources(reference).Replace("\\", "/");

            int index = normalized.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
                return normalized.Substring(index + "/Resources/".Length);

            if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring("Resources/".Length);

            if (normalized.StartsWith("../Resources/", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring("../Resources/".Length);

            return null;
        }

        private bool IsExternalOrSpecialReference(string reference)
        {
            string value = reference.Trim();

            return
                value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("#") ||
                value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);
        }

        private string MakeRelativePath(string fromFolder, string toFilePath)
        {
            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromFolder));
            Uri toUri = new Uri(toFilePath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);

            return Uri.UnescapeDataString(relativeUri.ToString());
        }

        private string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;

            return path;
        }

        private string NormalizeForFlarePath(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}