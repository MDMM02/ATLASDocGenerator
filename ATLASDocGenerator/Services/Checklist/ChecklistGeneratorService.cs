using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using B3.PluginAPIKit;

namespace ATLASDocGenerator.Services.Checklist
{
    public class ChecklistGeneratorService
    {
        private static readonly XNamespace MadCapNs =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        public string LastGeneratedFilePath { get; private set; }

        public List<ChecklistTargetInfo> GetAvailableTargets(string projectRoot)
        {
            ValidateProjectRoot(projectRoot);
            string targetRoot = Path.Combine(projectRoot, "Project", "Targets");
            if (!Directory.Exists(targetRoot))
                return new List<ChecklistTargetInfo>();

            List<ChecklistTargetInfo> result = new List<ChecklistTargetInfo>();
            foreach (string targetPath in Directory.GetFiles(
                targetRoot, "*.fltar", SearchOption.AllDirectories))
            {
                try
                {
                    XDocument target = XDocument.Load(targetPath, LoadOptions.PreserveWhitespace);
                    string tocPath = ResolveTargetTocPath(projectRoot, target);
                    if (!File.Exists(tocPath))
                        continue;

                    string documentReference = GetTargetVariable(
                        target, "General/DocumentReference") ?? string.Empty;
                    string relativeTarget = MakeRelativeFlarePath(targetRoot, targetPath);
                    result.Add(new ChecklistTargetInfo
                    {
                        TargetPath = targetPath,
                        TocPath = tocPath,
                        DocumentReference = documentReference,
                        DisplayName = string.IsNullOrWhiteSpace(documentReference)
                            ? relativeTarget
                            : relativeTarget + "  [" + documentReference + "]"
                    });
                }
                catch (XmlException)
                {
                    // Une target invalide n'est pas proposée dans le formulaire.
                }
                catch (InvalidOperationException)
                {
                    // Une target sans MasterToc exploitable n'est pas un document sélectionnable.
                }
            }

            return result.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public ChecklistGenerationResult Generate(ChecklistGenerationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            ValidateProjectRoot(request.ProjectRoot);
            if (string.IsNullOrWhiteSpace(request.SourceTargetPath)
                || !File.Exists(request.SourceTargetPath))
            {
                throw new FileNotFoundException(
                    "La target du document sélectionné est introuvable.",
                    request.SourceTargetPath);
            }

            XDocument sourceTarget = XDocument.Load(
                request.SourceTargetPath, LoadOptions.PreserveWhitespace);
            string sourceTocPath = ResolveTargetTocPath(request.ProjectRoot, sourceTarget);
            if (!File.Exists(sourceTocPath))
                throw new FileNotFoundException("La TOC de la target est introuvable.", sourceTocPath);

            XDocument sourceToc = XDocument.Load(sourceTocPath, LoadOptions.PreserveWhitespace);
            List<string> titles = ExtractDocumentChecklistActions(
                request.ProjectRoot,
                sourceToc,
                request.CreateNewDocument);
            if (titles.Count == 0)
                throw new InvalidOperationException("Aucun H1 admissible n'a été trouvé dans la TOC sélectionnée.");

            EnsureChecklistSnippets(request.ProjectRoot);

            return request.CreateNewDocument
                ? GenerateNewChecklistDocument(request, sourceTarget, titles)
                : AppendChecklistToExistingDocument(request.ProjectRoot, sourceTocPath, sourceToc, titles);
        }

        public int GenerateChecklistFromActiveDocument(IDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            return GenerateChecklistFromFile(ResolveFilePath(document.GetSourceUrl()));
        }

        internal int GenerateChecklistFromFile(string activeTopicPath)
        {
            if (string.IsNullOrWhiteSpace(activeTopicPath) || !File.Exists(activeTopicPath))
                throw new FileNotFoundException("The active topic could not be found.", activeTopicPath);

            string projectRoot = ResolveProjectRoot(activeTopicPath);
            string tocPath = FindDocumentToc(projectRoot, activeTopicPath);
            XDocument toc = XDocument.Load(tocPath, LoadOptions.PreserveWhitespace);
            List<string> titles = ExtractDocumentChecklistActions(projectRoot, toc, false);
            if (titles.Count == 0)
                throw new InvalidOperationException("Aucun H1 admissible n'a été trouvé dans la TOC sélectionnée.");

            EnsureChecklistSnippets(projectRoot);
            ChecklistGenerationResult result = AppendChecklistToExistingDocument(
                projectRoot, tocPath, toc, titles);
            return result.SectionCount;
        }

        private ChecklistGenerationResult AppendChecklistToExistingDocument(
            string projectRoot,
            string tocPath,
            XDocument toc,
            List<string> titles)
        {
            string documentFolder = FindMainDocumentFolder(projectRoot, toc);
            string checklistPath = Path.Combine(documentFolder, "Checklist.htm");
            WriteChecklistTopic(projectRoot, checklistPath, titles);
            AddChecklistToToc(projectRoot, tocPath, toc, checklistPath);
            LastGeneratedFilePath = checklistPath;

            return new ChecklistGenerationResult
            {
                SectionCount = titles.Count,
                ChecklistTopicPath = checklistPath,
                TocPath = tocPath
            };
        }

        private ChecklistGenerationResult GenerateNewChecklistDocument(
            ChecklistGenerationRequest request,
            XDocument sourceTarget,
            List<string> titles)
        {
            if (string.IsNullOrWhiteSpace(request.NewDocumentReference))
                throw new InvalidOperationException("Document Reference est obligatoire.");

            string safeReference = FileNameSanitizer.ToSafeName(request.NewDocumentReference);
            if (string.IsNullOrWhiteSpace(safeReference))
                throw new InvalidOperationException("Document Reference ne permet pas de créer un nom de fichier valide.");

            string documentName = safeReference + "_checklist";
            string documentFolder = Path.Combine(request.ProjectRoot, "Content", documentName);
            string sourceTargetFolder = Path.GetDirectoryName(request.SourceTargetPath);
            string sourceTocPath = ResolveTargetTocPath(request.ProjectRoot, sourceTarget);
            string sourceTocFolder = Path.GetDirectoryName(sourceTocPath);
            string newTargetPath = Path.Combine(sourceTargetFolder, documentName + ".fltar");
            string newTocPath = Path.Combine(sourceTocFolder, documentName + ".fltoc");
            string checklistPath = Path.Combine(documentFolder, "Checklist.htm");

            if (Directory.Exists(documentFolder))
                throw new IOException("Le dossier du nouveau document existe déjà :\n" + documentFolder);
            if (File.Exists(newTargetPath))
                throw new IOException("La target existe déjà :\n" + newTargetPath);
            if (File.Exists(newTocPath))
                throw new IOException("La TOC existe déjà :\n" + newTocPath);

            try
            {
                Directory.CreateDirectory(documentFolder);
                WriteChecklistTopic(request.ProjectRoot, checklistPath, titles);

                XDocument newToc = CreateStandaloneChecklistToc(
                    request.ProjectRoot,
                    checklistPath,
                    titles);
                SaveXml(newTocPath, newToc);

                XDocument newTarget = new XDocument(sourceTarget);
                SetTargetVariable(
                    newTarget,
                    "General/DocumentReference",
                    request.NewDocumentReference.Trim());
                newTarget.Root.SetAttributeValue(
                    "MasterToc",
                    "/" + MakeRelativeFlarePath(request.ProjectRoot, newTocPath));
                newTarget.Save(newTargetPath, SaveOptions.DisableFormatting);

                LastGeneratedFilePath = checklistPath;
                return new ChecklistGenerationResult
                {
                    SectionCount = titles.Count,
                    ChecklistTopicPath = checklistPath,
                    TocPath = newTocPath,
                    TargetPath = newTargetPath
                };
            }
            catch
            {
                if (File.Exists(newTargetPath))
                    File.Delete(newTargetPath);
                if (File.Exists(newTocPath))
                    File.Delete(newTocPath);
                if (Directory.Exists(documentFolder))
                    Directory.Delete(documentFolder, true);
                throw;
            }
        }

        private List<string> ExtractDocumentChecklistActions(
            string projectRoot,
            XDocument toc,
            bool excludeSummary)
        {
            List<string> titles = new List<string>();
            foreach (string link in GetTopicLinks(toc))
            {
                string topicPath = ResolveContentLink(projectRoot, link);
                if (topicPath == null || !File.Exists(topicPath)
                    || Path.GetFileName(topicPath).Equals("Checklist.htm", StringComparison.OrdinalIgnoreCase))
                    continue;

                XDocument topic = XDocument.Load(topicPath, LoadOptions.PreserveWhitespace);
                if (IsPrerequisiteTopic(topicPath, topic))
                {
                    titles.AddRange(topic.Descendants()
                        .Where(element => element.Name.LocalName.Equals(
                            "p", StringComparison.OrdinalIgnoreCase))
                        .Where(element => HasClass(element, "ss_section"))
                        .Select(element => NormalizeText(element.Value))
                        .Where(title => !string.IsNullOrWhiteSpace(title)));
                    continue;
                }

                foreach (XElement h1 in topic.Descendants().Where(element =>
                    element.Name.LocalName.Equals("h1", StringComparison.OrdinalIgnoreCase)))
                {
                    string title = NormalizeText(h1.Value);
                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    if (IsNonNumberedH1(h1))
                        continue;
                    if (excludeSummary
                        && title.Equals("Sommaire", StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    titles.Add(title);
                }
            }

            return titles;
        }

        private bool IsNonNumberedH1(XElement h1)
        {
            XAttribute classAttribute = h1.Attribute("class");
            if (classAttribute == null)
                return false;

            string[] classes = classAttribute.Value.Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return classes.Any(value =>
                value.Equals("no_num", StringComparison.OrdinalIgnoreCase)
                || value.Equals("non_numerote", StringComparison.OrdinalIgnoreCase)
                || value.Equals("non_numéroté", StringComparison.OrdinalIgnoreCase)
                || value.Equals("non_num", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsPrerequisite(string topicPath, string title)
        {
            string fileName = Path.GetFileNameWithoutExtension(topicPath) ?? string.Empty;
            return title.Equals("Prérequis", StringComparison.CurrentCultureIgnoreCase)
                || title.Equals("Pré-requis", StringComparison.CurrentCultureIgnoreCase)
                || fileName.Equals("Prérequis", StringComparison.CurrentCultureIgnoreCase)
                || fileName.Equals("Pré-requis", StringComparison.CurrentCultureIgnoreCase);
        }

        private bool IsPrerequisiteTopic(string topicPath, XDocument topic)
        {
            string fileName = Path.GetFileNameWithoutExtension(topicPath) ?? string.Empty;
            return fileName.Equals("Prérequis", StringComparison.CurrentCultureIgnoreCase)
                || fileName.Equals("Pré-requis", StringComparison.CurrentCultureIgnoreCase)
                || topic.Descendants().Any(element =>
                    element.Name.LocalName.Equals("h1", StringComparison.OrdinalIgnoreCase)
                    && (NormalizeText(element.Value).Equals(
                            "Prérequis", StringComparison.CurrentCultureIgnoreCase)
                        || NormalizeText(element.Value).Equals(
                            "Pré-requis", StringComparison.CurrentCultureIgnoreCase)));
        }

        private bool HasClass(XElement element, string className)
        {
            XAttribute classAttribute = element.Attribute("class");
            return classAttribute != null
                && classAttribute.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(value => value.Equals(className, StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureChecklistSnippets(string projectRoot)
        {
            string qiqoFolder = GetQiqoFolder(projectRoot);
            string qiqoTablePath = Path.Combine(qiqoFolder, "QIQO_table.flsnp");
            if (!File.Exists(qiqoTablePath))
                throw new FileNotFoundException("Le snippet QIQO_table est introuvable.", qiqoTablePath);

            WriteTextSnippet(
                Path.Combine(qiqoFolder, "intro_checklist.flsnp"),
                "Compléter la checklist ci-dessous avant de clôturer l’intervention. "
                + "Si NOK ou N/A est sélectionné, ajouter un commentaire.");
            WriteTextSnippet(
                Path.Combine(qiqoFolder, "titre_checklist.flsnp"),
                "Checklist");
        }

        private void WriteTextSnippet(string path, string text)
        {
            XDocument snippet = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("html",
                    new XAttribute(XNamespace.Xmlns + "MadCap", MadCapNs),
                    new XElement("body", text)));
            SaveWithInitialBackup(path, snippet);
        }

        private void WriteChecklistTopic(string projectRoot, string checklistPath, IEnumerable<string> titles)
        {
            string checklistFolder = Path.GetDirectoryName(checklistPath);
            Directory.CreateDirectory(checklistFolder);
            string qiqoFolder = GetQiqoFolder(projectRoot);
            string intro = MakeRelativeFlarePath(
                checklistFolder, Path.Combine(qiqoFolder, "intro_checklist.flsnp"));
            string title = MakeRelativeFlarePath(
                checklistFolder, Path.Combine(qiqoFolder, "titre_checklist.flsnp"));
            string table = MakeRelativeFlarePath(
                checklistFolder, Path.Combine(qiqoFolder, "QIQO_table.flsnp"));

            XElement body = new XElement("body",
                new XElement("h1",
                    new XElement(MadCapNs + "snippetText", new XAttribute("src", title))),
                new XElement("p",
                    new XElement(MadCapNs + "snippetText", new XAttribute("src", intro))));

            XElement numberedActions = new XElement(
                "ol",
                new XAttribute("class", "Action_num"));

            foreach (string heading in titles)
            {
                int stepNumber = numberedActions.Elements("li").Count() + 1;
                numberedActions.Add(new XElement(
                    "li",
                    new XAttribute("id", "checklist-step-" + stepNumber.ToString("D3")),
                    new XElement("p", heading),
                    new XElement(
                        MadCapNs + "snippetBlock",
                        new XAttribute("src", table))));
            }

            body.Add(numberedActions);

            XDocument topic = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("html",
                    new XAttribute(XNamespace.Xmlns + "MadCap", MadCapNs),
                    new XElement("head", new XElement("title", "Checklist")),
                    body));
            SaveWithInitialBackup(checklistPath, topic);
        }

        private void AddChecklistToToc(
            string projectRoot, string tocPath, XDocument toc, string checklistPath)
        {
            bool alreadyPresent = GetTopicLinks(toc).Any(link =>
            {
                string resolved = ResolveContentLink(projectRoot, link);
                return resolved != null && PathsAreEqual(resolved, checklistPath);
            });
            if (alreadyPresent)
                return;
            if (toc.Root == null)
                throw new InvalidOperationException("La TOC ne possède pas de racine XML.");

            toc.Root.Add(CreateChecklistTocEntry(projectRoot, checklistPath));
            SaveWithInitialBackup(tocPath, toc);
        }

        private XElement CreateChecklistTocEntry(string projectRoot, string checklistPath)
        {
            string contentRoot = Path.Combine(projectRoot, "Content");
            return new XElement("TocEntry",
                new XAttribute("Title", "Checklist"),
                new XAttribute("Link", "/Content/" + MakeRelativeFlarePath(contentRoot, checklistPath)));
        }

        private XDocument CreateStandaloneChecklistToc(
            string projectRoot,
            string checklistPath,
            IList<string> titles)
        {
            XElement checklistEntry = CreateChecklistTocEntry(projectRoot, checklistPath);
            string baseLink = (string)checklistEntry.Attribute("Link");

            for (int index = 0; index < titles.Count; index++)
            {
                checklistEntry.Add(new XElement(
                    "TocEntry",
                    new XAttribute("Title", (index + 1) + ". " + titles[index]),
                    new XAttribute(
                        "Link",
                        baseLink + "#checklist-step-" + (index + 1).ToString("D3"))));
            }

            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    "CatapultToc",
                    new XAttribute("Version", "1"),
                    checklistEntry));
        }

        private void RemoveChecklistEntries(string projectRoot, XDocument toc)
        {
            foreach (XElement entry in toc.Descendants()
                .Where(element => element.Name.LocalName.Equals(
                    "TocEntry", StringComparison.OrdinalIgnoreCase))
                .Where(element =>
                {
                    XAttribute link = element.Attributes().FirstOrDefault(attribute =>
                        attribute.Name.LocalName.Equals("Link", StringComparison.OrdinalIgnoreCase));
                    XAttribute title = element.Attributes().FirstOrDefault(attribute =>
                        attribute.Name.LocalName.Equals("Title", StringComparison.OrdinalIgnoreCase));
                    string resolved = link == null ? null : ResolveContentLink(projectRoot, link.Value);
                    return (resolved != null
                            && Path.GetFileName(resolved).Equals("Checklist.htm", StringComparison.OrdinalIgnoreCase))
                        || (title != null
                            && title.Value.Equals("Checklist", StringComparison.OrdinalIgnoreCase));
                })
                .ToList())
            {
                entry.Remove();
            }
        }

        private string FindMainDocumentFolder(string projectRoot, XDocument toc)
        {
            string contentRoot = Path.Combine(projectRoot, "Content");
            List<string> folders = GetTopicLinks(toc)
                .Select(link => ResolveContentLink(projectRoot, link))
                .Where(path => path != null && File.Exists(path))
                .Select(Path.GetDirectoryName)
                .Where(folder => folder != null)
                .Where(folder => !folder.StartsWith(
                    Path.Combine(contentRoot, "Resources"), StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (folders.Count == 0)
                throw new InvalidOperationException("Impossible de déterminer le dossier principal du document.");

            return folders.GroupBy(folder => folder, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .First().Key;
        }

        private string ResolveTargetTocPath(string projectRoot, XDocument target)
        {
            if (target.Root == null)
                throw new InvalidOperationException("La target ne possède pas de racine XML.");
            XAttribute masterToc = target.Root.Attributes().FirstOrDefault(attribute =>
                attribute.Name.LocalName.Equals("MasterToc", StringComparison.OrdinalIgnoreCase));
            if (masterToc == null || string.IsNullOrWhiteSpace(masterToc.Value))
                throw new InvalidOperationException("La target ne définit pas MasterToc.");

            string relative = Uri.UnescapeDataString(masterToc.Value.TrimStart('/', '\\'))
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relative));
            string projectPath = Path.GetFullPath(Path.Combine(projectRoot, "Project"));
            if (!fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La MasterToc sort du dossier Project.");
            return fullPath;
        }

        private string GetTargetVariable(XDocument target, string variableName)
        {
            XElement variable = target.Descendants().FirstOrDefault(element =>
                element.Name.LocalName.Equals("Variable", StringComparison.OrdinalIgnoreCase)
                && element.Attributes().Any(attribute =>
                    attribute.Name.LocalName.Equals("Name", StringComparison.OrdinalIgnoreCase)
                    && attribute.Value.Equals(variableName, StringComparison.OrdinalIgnoreCase)));
            return variable == null ? null : variable.Value;
        }

        private void SetTargetVariable(XDocument target, string variableName, string value)
        {
            XElement variable = target.Descendants().FirstOrDefault(element =>
                element.Name.LocalName.Equals("Variable", StringComparison.OrdinalIgnoreCase)
                && element.Attributes().Any(attribute =>
                    attribute.Name.LocalName.Equals("Name", StringComparison.OrdinalIgnoreCase)
                    && attribute.Value.Equals(variableName, StringComparison.OrdinalIgnoreCase)));
            if (variable == null)
            {
                XElement variables = target.Root.Elements().FirstOrDefault(element =>
                    element.Name.LocalName.Equals("Variables", StringComparison.OrdinalIgnoreCase));
                if (variables == null)
                {
                    variables = new XElement(target.Root.Name.Namespace + "Variables");
                    target.Root.Add(variables);
                }
                variable = new XElement(variables.Name.Namespace + "Variable",
                    new XAttribute("Name", variableName));
                variables.Add(variable);
            }
            variable.Value = value;
        }

        private IEnumerable<string> GetTopicLinks(XDocument toc)
        {
            return toc.Descendants()
                .SelectMany(element => element.Attributes())
                .Where(attribute => attribute.Name.LocalName.Equals("Link", StringComparison.OrdinalIgnoreCase))
                .Select(attribute => attribute.Value)
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Where(link =>
                {
                    string clean = link.Split('#', '?')[0];
                    return clean.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)
                        || clean.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
                });
        }

        private string ResolveContentLink(string projectRoot, string link)
        {
            string clean = link.Replace('\\', '/').Split('#', '?')[0];
            if (!clean.StartsWith("/Content/", StringComparison.OrdinalIgnoreCase))
                return null;
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                Uri.UnescapeDataString(clean.TrimStart('/')).Replace('/', Path.DirectorySeparatorChar)));
        }

        private string FindDocumentToc(string projectRoot, string activeTopicPath)
        {
            string tocFolder = Path.Combine(projectRoot, "Project", "TOCs");
            List<string> matches = new List<string>();
            foreach (string tocPath in Directory.GetFiles(tocFolder, "*.fltoc", SearchOption.AllDirectories))
            {
                XDocument toc = XDocument.Load(tocPath, LoadOptions.PreserveWhitespace);
                if (GetTopicLinks(toc).Select(link => ResolveContentLink(projectRoot, link))
                    .Any(path => path != null && PathsAreEqual(path, activeTopicPath)))
                    matches.Add(tocPath);
            }
            if (matches.Count != 1)
                throw new InvalidOperationException(matches.Count == 0
                    ? "Aucune TOC ne référence le topic actif."
                    : "Plusieurs TOC référencent le topic actif.");
            return matches[0];
        }

        private string GetQiqoFolder(string projectRoot)
        {
            return Path.Combine(projectRoot, "Content", "Resources", "Commun Stago", "QIQO_content");
        }

        private void SaveWithInitialBackup(string path, XDocument document)
        {
            if (File.Exists(path) && !File.Exists(path + ".before-checklist.bak"))
                File.Copy(path, path + ".before-checklist.bak", false);
            SaveXml(path, document);
        }

        private void SaveXml(string path, XDocument document)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = false
            };
            using (XmlWriter writer = XmlWriter.Create(path, settings))
                document.Save(writer);
        }

        private void ValidateProjectRoot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
                throw new DirectoryNotFoundException("La racine du projet Flare est introuvable.");
        }

        private string ResolveProjectRoot(string topicPath)
        {
            DirectoryInfo directory = new FileInfo(topicPath).Directory;
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Content"))
                    && Directory.Exists(Path.Combine(directory.FullName, "Project")))
                    return directory.FullName;
                directory = directory.Parent;
            }
            throw new InvalidOperationException("Impossible de retrouver la racine du projet Flare.");
        }

        private string MakeRelativeFlarePath(string fromFolder, string targetPath)
        {
            Uri from = new Uri(AppendDirectorySeparator(Path.GetFullPath(fromFolder)));
            Uri target = new Uri(Path.GetFullPath(targetPath));
            return Uri.UnescapeDataString(from.MakeRelativeUri(target).ToString()).Replace('\\', '/');
        }

        private string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private bool PathsAreEqual(string first, string second)
        {
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveFilePath(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new InvalidOperationException("The active document has no source URL.");
            Uri uri;
            return Uri.TryCreate(sourceUrl, UriKind.Absolute, out uri) && uri.IsFile
                ? uri.LocalPath
                : sourceUrl;
        }

        private string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return string.Join(" ", value.Split(
                new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
