using System;
using System.IO;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Crée une sauvegarde initiale immuable avant qu'un service ATLAS
    /// ne modifie un fichier existant.
    /// </summary>
    public static class FileBackupService
    {
        public static bool CreateInitialBackup(
            string filePath,
            string backupSuffix)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Le chemin du fichier à sauvegarder est vide.",
                    "filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Le fichier à sauvegarder est introuvable.",
                    filePath);
            }

            if (string.IsNullOrWhiteSpace(backupSuffix))
            {
                throw new ArgumentException(
                    "Le suffixe de sauvegarde est vide.",
                    "backupSuffix");
            }

            string backupPath = filePath + backupSuffix;

            if (File.Exists(backupPath))
            {
                return false;
            }

            File.Copy(filePath, backupPath, false);
            return true;
        }
    }
}
