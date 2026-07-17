using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe récupère la liste des fichiers .htm à traiter
    /// Elle filtre aussi les dossiers qui ne doivent pas être traités, comme Output, Analyzer ou Project.
    /// </summary>

    public class HtmlFileScanner
    {
        /// <summary>
        /// Récupère ous les fichiers .htm à traiter selon les options utilisateur
        /// Traitement:
        /// 1. Vérifie que les options et le chemin cible sont valides
        /// 2. Détermine le dossier racine à scanner
        /// 3. Récupère les fichiers .htm dans ce dossier et ses sous-dossiers
        /// 4. Exclut les fichiers situés dans Output, Analyzer et Project
        /// 5. Retourne la liste triée des fichiers à traiter
        /// </summary>
        /// <param name="options"></param> options sélectionnées ds la fenêtre (dossier/projet complet)
        /// <param name="scanRoot"></param> dossier réellement scanné par le cleanup.
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public List<string> GetHtmlFiles(AitCleanupOptions options, out string scanRoot)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (string.IsNullOrWhiteSpace(options.TargetPath))
            {
                throw new InvalidOperationException("No target path was provided.");
            }

            if (!Directory.Exists(options.TargetPath))
            {
                throw new DirectoryNotFoundException("Target folder not found: " + options.TargetPath);
            }

            scanRoot = ResolveScanRoot(options);

            if (!Directory.Exists(scanRoot))
            {
                throw new DirectoryNotFoundException("Scan root not found: " + scanRoot);
            }

            List<string> files = Directory
                .EnumerateFiles(scanRoot, "*.htm", SearchOption.AllDirectories)
                .Where(IsValidTopicFile)
                .OrderBy(path => path)
                .ToList();

            return files;
        }

        /// <summary>
        /// Détermine le dossier racine à scanner,      
        /// si l'utilisateur choisi 'Selected folder', on scanne directement le dossier sélectionné.
        /// s'il choisit 'Whole Project', on essaie de scanner le dossier Content du projet.
        /// </summary>
        
        private string ResolveScanRoot(AitCleanupOptions options)
        {
            if (options.Scope == AitCleanupScope.SelectedFolder)
            {
                return options.TargetPath;
            }

            string folderName = new DirectoryInfo(options.TargetPath).Name;

            if (folderName.Equals("Content", StringComparison.OrdinalIgnoreCase))
            {
                return options.TargetPath;
            }

            string contentFolder = Path.Combine(options.TargetPath, "Content");

            if (Directory.Exists(contentFolder))
            {
                return contentFolder;
            }

            // Fallback: scan selected folder.
            return options.TargetPath;
        }
        /// <summary>
        /// Vérifie si un fichier trouvé peut être traité comme un topic
        /// Certains dossiers d'un projet Flare ne doivent pas être modifiés (output, analyzer, project)
        /// </summary>
        /// <param name="filePath"></param>Chemin du fichier àvérifier
        /// <returns></returns>
        private bool IsValidTopicFile(string filePath)
        {
            // Normalise les séparateurs pour rendre les tests plus robustes
            string normalized = filePath.Replace('/', '\\');

            if (normalized.IndexOf("\\Output\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("\\Analyzer\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("\\Project\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }
    }
}