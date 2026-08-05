using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services.AitCleanup.IhmVariables
{
    /// <summary>
    /// Remplace les références MadCap:snippetText pointant vers
    /// Topic&lt;ID&gt;.flsnp par des références MadCap:variable.
    /// </summary>
    public class IhmVariableReferenceTransformer
    {
        private const string MadCapNamespace =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        private static readonly Regex TopicSnippetRegex =
            new Regex(
                @"^Topic(?<id>\d+)$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Scanne tous les fichiers .htm, .html et .flsnp du dossier Content
        /// et remplace les snippetText correspondant aux variables générées.
        /// </summary>
        public IhmVariableReferenceTransformResult Transform(
            string selectedPath,
            IEnumerable<FrenchIhmVariableSetGenerationResult> variableSetResults)
        {
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                throw new ArgumentException(
                    "Le chemin du projet Flare est vide.",
                    "selectedPath");
            }

            if (!Directory.Exists(selectedPath))
            {
                throw new DirectoryNotFoundException(
                    "Le dossier sélectionné est introuvable : "
                    + selectedPath);
            }

            if (variableSetResults == null)
            {
                throw new ArgumentNullException(
                    "variableSetResults");
            }

            List<FrenchIhmVariableSetGenerationResult> results =
                variableSetResults
                    .Where(result => result != null)
                    .ToList();

            if (results.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucun résultat de génération de variables IHM "
                    + "n'a été fourni au transformer.");
            }

            string flareProjectRoot =
                ResolveFlareProjectRoot(selectedPath);

            string contentRoot =
                Path.Combine(flareProjectRoot, "Content");

            Dictionary<string, IhmVariableReferenceTarget> mapping =
                BuildTopicIdMapping(results);

            IhmVariableReferenceTransformResult transformResult =
                new IhmVariableReferenceTransformResult
                {
                    FlareProjectRootPath = flareProjectRoot,
                    ContentRootPath = contentRoot,
                    MappingEntries = mapping.Count
                };

            List<string> files =
                Directory
                    .EnumerateFiles(
                        contentRoot,
                        "*.*",
                        SearchOption.AllDirectories)
                    .Where(IsSupportedContentFile)
                    .ToList();

            transformResult.FilesScanned = files.Count;

            foreach (string filePath in files)
            {
                TransformFile(
                    filePath,
                    mapping,
                    transformResult);
            }

            return transformResult;
        }

        private Dictionary<string, IhmVariableReferenceTarget>
            BuildTopicIdMapping(
                IEnumerable<FrenchIhmVariableSetGenerationResult> results)
        {
            Dictionary<string, IhmVariableReferenceTarget> mapping =
                new Dictionary<string, IhmVariableReferenceTarget>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (FrenchIhmVariableSetGenerationResult result in results)
            {
                if (string.IsNullOrWhiteSpace(result.VariableSetName))
                {
                    throw new InvalidOperationException(
                        "Un résultat IHM ne possède pas de nom de VariableSet.");
                }

                if (result.TopicIdToVariableName == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> item
                    in result.TopicIdToVariableName)
                {
                    string topicId =
                        item.Key == null
                            ? string.Empty
                            : item.Key.Trim();

                    string variableName =
                        item.Value == null
                            ? string.Empty
                            : item.Value.Trim();

                    if (string.IsNullOrWhiteSpace(topicId)
                        || string.IsNullOrWhiteSpace(variableName))
                    {
                        continue;
                    }

                    IhmVariableReferenceTarget newTarget =
                        new IhmVariableReferenceTarget
                        {
                            TopicId = topicId,
                            VariableSetName = result.VariableSetName,
                            VariableName = variableName
                        };

                    IhmVariableReferenceTarget existingTarget;

                    if (mapping.TryGetValue(
                        topicId,
                        out existingTarget))
                    {
                        bool sameReference =
                            string.Equals(
                                existingTarget.FullVariableName,
                                newTarget.FullVariableName,
                                StringComparison.OrdinalIgnoreCase);

                        if (!sameReference)
                        {
                            throw new InvalidOperationException(
                                "L'ID Author-it "
                                + topicId
                                + " correspond à plusieurs variables : "
                                + existingTarget.FullVariableName
                                + " et "
                                + newTarget.FullVariableName
                                + ".");
                        }

                        continue;
                    }

                    mapping.Add(
                        topicId,
                        newTarget);
                }
            }

            return mapping;
        }

        private void TransformFile(
            string filePath,
            Dictionary<string, IhmVariableReferenceTarget> mapping,
            IhmVariableReferenceTransformResult result)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(
                    filePath,
                    LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                result.Errors.Add(
                    filePath
                    + " : impossible de lire le fichier XML/XHTML. "
                    + ex.Message);

                return;
            }

            if (document.Root == null)
            {
                result.Errors.Add(
                    filePath
                    + " : le fichier ne contient pas d'élément racine.");

                return;
            }

            List<XElement> snippetReferences =
                document
                    .Descendants()
                    .Where(element =>
                        string.Equals(
                            element.Name.LocalName,
                            "snippetText",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (snippetReferences.Count == 0)
            {
                return;
            }

            int replacementsInFile = 0;
            List<string> replacementDetails = new List<string>();

            foreach (XElement snippetReference in snippetReferences)
            {
                XAttribute srcAttribute =
                    snippetReference
                        .Attributes()
                        .FirstOrDefault(attribute =>
                            string.Equals(
                                attribute.Name.LocalName,
                                "src",
                                StringComparison.OrdinalIgnoreCase));

                if (srcAttribute == null
                    || string.IsNullOrWhiteSpace(srcAttribute.Value))
                {
                    continue;
                }

                string topicId =
                    ExtractTopicIdFromSnippetPath(
                        srcAttribute.Value);

                if (string.IsNullOrWhiteSpace(topicId))
                {
                    continue;
                }

                IhmVariableReferenceTarget target;

                if (!mapping.TryGetValue(
                    topicId,
                    out target))
                {
                    result.UnmatchedTopicIds.Add(topicId);
                    continue;
                }

                XElement variableReference =
                    new XElement(
                        XName.Get(
                            "variable",
                            MadCapNamespace),
                        new XAttribute(
                            "name",
                            target.FullVariableName),
                        new XAttribute(
                            "class",
                            "IHM"));

                snippetReference.ReplaceWith(
                    variableReference);

                replacementsInFile++;

                replacementDetails.Add(
                    GetRelativePath(
                        result.ContentRootPath,
                        filePath)
                    + " | Topic"
                    + topicId
                    + ".flsnp -> "
                    + target.FullVariableName);
            }

            if (replacementsInFile == 0)
            {
                return;
            }

            try
            {
                CreateBackup(filePath);

                document.Save(
                    filePath,
                    SaveOptions.DisableFormatting);

                result.FilesModified++;
                result.ReferencesReplaced += replacementsInFile;
                result.Details.AddRange(replacementDetails);
            }
            catch (Exception ex)
            {
                result.Errors.Add(
                    filePath
                    + " : impossible d'enregistrer les remplacements. "
                    + ex.Message);
            }
        }

        private string ExtractTopicIdFromSnippetPath(
            string snippetPath)
        {
            if (string.IsNullOrWhiteSpace(snippetPath))
            {
                return string.Empty;
            }

            string normalizedPath =
                snippetPath
                    .Trim()
                    .Replace('\\', '/');

            int lastSlashIndex =
                normalizedPath.LastIndexOf('/');

            string fileName =
                lastSlashIndex >= 0
                    ? normalizedPath.Substring(lastSlashIndex + 1)
                    : normalizedPath;

            string fileNameWithoutExtension =
                Path.GetFileNameWithoutExtension(fileName);

            Match match =
                TopicSnippetRegex.Match(
                    fileNameWithoutExtension);

            if (!match.Success)
            {
                return string.Empty;
            }

            return match
                .Groups["id"]
                .Value;
        }

        private bool IsSupportedContentFile(
            string filePath)
        {
            string extension =
                Path.GetExtension(filePath);

            return string.Equals(
                       extension,
                       ".htm",
                       StringComparison.OrdinalIgnoreCase)
                   || string.Equals(
                       extension,
                       ".html",
                       StringComparison.OrdinalIgnoreCase)
                   || string.Equals(
                       extension,
                       ".flsnp",
                       StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveFlareProjectRoot(
            string selectedPath)
        {
            DirectoryInfo current =
                new DirectoryInfo(selectedPath);

            while (current != null)
            {
                string projectFolder =
                    Path.Combine(
                        current.FullName,
                        "Project");

                string contentFolder =
                    Path.Combine(
                        current.FullName,
                        "Content");

                if (Directory.Exists(projectFolder)
                    && Directory.Exists(contentFolder))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                "Impossible de retrouver la racine du projet Flare depuis : "
                + selectedPath
                + Environment.NewLine
                + "La racine doit contenir les dossiers Project et Content.");
        }

        private void CreateBackup(
            string filePath)
        {
            string backupPath =
                filePath
                + ".before-ihm-variables."
                + DateTime.Now.ToString("yyyyMMdd_HHmmssfff")
                + ".bak";

            File.Copy(
                filePath,
                backupPath,
                false);
        }

        private string GetRelativePath(
            string basePath,
            string fullPath)
        {
            if (string.IsNullOrWhiteSpace(basePath)
                || string.IsNullOrWhiteSpace(fullPath))
            {
                return fullPath;
            }

            string normalizedBase =
                Path.GetFullPath(basePath)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            string normalizedFull =
                Path.GetFullPath(fullPath);

            Uri baseUri =
                new Uri(normalizedBase);

            Uri fullUri =
                new Uri(normalizedFull);

            return Uri
                .UnescapeDataString(
                    baseUri.MakeRelativeUri(fullUri).ToString())
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);
        }
    }

    public class IhmVariableReferenceTarget
    {
        public string TopicId { get; set; }

        public string VariableSetName { get; set; }

        public string VariableName { get; set; }

        public string FullVariableName
        {
            get
            {
                return (VariableSetName ?? string.Empty)
                    + "."
                    + (VariableName ?? string.Empty);
            }
        }
    }

    public class IhmVariableReferenceTransformResult
    {
        public IhmVariableReferenceTransformResult()
        {
            Details = new List<string>();
            Errors = new List<string>();

            UnmatchedTopicIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
        }

        public string FlareProjectRootPath { get; set; }

        public string ContentRootPath { get; set; }

        public int MappingEntries { get; set; }

        public int FilesScanned { get; set; }

        public int FilesModified { get; set; }

        public int ReferencesReplaced { get; set; }

        public List<string> Details { get; private set; }

        public List<string> Errors { get; private set; }

        public HashSet<string> UnmatchedTopicIds { get; private set; }
    }
}
