using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using B3.PluginAPIKit;

namespace ATLASDocGenerator.Services
{
    internal static class FlareProjectContextService
    {
        internal static string ResolveProjectRoot(IDocument document)
        {
            if (document == null)
            {
                throw new InvalidOperationException(
                    "Ouvrez un topic du projet Flare avant de lancer le générateur.");
            }

            string sourceUrl = document.GetSourceUrl();
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                throw new InvalidOperationException(
                    "Le topic actif ne possède pas de chemin source.");
            }

            Uri uri;
            string filePath = Uri.TryCreate(sourceUrl, UriKind.Absolute, out uri)
                && uri.IsFile
                    ? uri.LocalPath
                    : sourceUrl;

            return ResolveProjectRootFromPath(filePath);
        }

        internal static string ResolveProjectRootFromPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Le chemin du topic est vide.", "filePath");

            DirectoryInfo directory = new FileInfo(filePath).Directory;
            while (directory != null)
            {
                bool hasContent = Directory.Exists(Path.Combine(directory.FullName, "Content"));
                bool hasProject = Directory.Exists(Path.Combine(directory.FullName, "Project"));
                bool hasFlareProject = directory.Exists
                    && Directory.GetFiles(
                        directory.FullName,
                        "*.flprj",
                        SearchOption.TopDirectoryOnly).Length > 0;

                if (hasContent && hasProject && hasFlareProject)
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Impossible de retrouver la racine du projet à partir du topic actif. "
                + "Ouvrez un topic appartenant au projet Flare courant.");
        }

        internal static List<string> LoadDeviceNames(string projectRoot)
        {
            string variableSetPath = Path.Combine(
                projectRoot,
                "Project",
                "VariableSets",
                "General.flvar");

            if (!File.Exists(variableSetPath))
            {
                throw new FileNotFoundException(
                    "Le jeu de variables General.flvar est introuvable.",
                    variableSetPath);
            }

            XDocument document = XDocument.Load(variableSetPath);
            List<string> devices = document
                .Descendants()
                .Where(element => element.Name.LocalName.Equals(
                    "Variable",
                    StringComparison.OrdinalIgnoreCase))
                .Where(element =>
                {
                    XAttribute name = element.Attributes().FirstOrDefault(attribute =>
                        attribute.Name.LocalName.Equals("Name", StringComparison.OrdinalIgnoreCase));
                    return name != null
                        && name.Value.Equals("dispositif", StringComparison.OrdinalIgnoreCase);
                })
                .Select(element => (element.Value ?? string.Empty).Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Where(value => !value.Equals("Nom du dispositif", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!devices.Contains("Multi", StringComparer.OrdinalIgnoreCase))
            {
                devices.Add("Multi");
            }

            if (!devices.Contains("Autre", StringComparer.OrdinalIgnoreCase))
            {
                devices.Add("Autre");
            }

            return devices;
        }
    }
}
