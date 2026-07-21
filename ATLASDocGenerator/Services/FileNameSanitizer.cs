using System.Globalization;
using System.IO;
using System.Text;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Cette classe fournit une méthode de normalisation pour créer des noms de fichiers et de dossiers compatibles avec les règles du projet ATLAS Doc Generator.
    ///
    /// La normalisation applique les règles suivantes :
    /// - suppression des espaces placés au début et à la fin
    /// - suppression des accents
    /// - remplacement des espaces par des underscores
    /// - suppression des caractères interdits par Windows
    /// - conservation uniquement des lettres, chiffres, tirets et underscores
    ///
    /// Exemples :
    /// "Réglages système" devient "Reglages_systeme"
    /// "BT-001 (Révision A)" devient "BT-001_Revision_A"
    /// </summary>
    public static class FileNameSanitizer
    {
        /// <summary>
        /// Transforme une chaîne en nom compatible avec les règles de nommage des fichiers et dossiers du projet.
        /// </summary>
        /// <param name="value">Texte à normaliser.</param>
        /// <returns>
        /// Nom nettoyé contenant uniquement des lettres ASCII,
        /// des chiffres, des tirets et des underscores.
        /// </returns>
        public static string ToSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // Supprime les espaces placés au début et à la fin, puis retire les accents.
            string cleaned = RemoveDiacritics(value.Trim());

            // Les espaces sont remplacés par des underscores.
            cleaned = cleaned.Replace(" ", "_");

            // Récupère les caractères interdits dans les noms de fichiers Windows.
            char[] invalidChars = Path.GetInvalidFileNameChars();

            StringBuilder builder = new StringBuilder();

            foreach (char character in cleaned)
            {
                // Ignore les caractères explicitement interdits dans un nom de fichier Windows.
                if (System.Array.IndexOf(invalidChars, character) >= 0)
                {
                    continue;
                }

                // Conserve uniquement :
                // - les lettres de A à Z et de a à z
                // - les chiffres de 0 à 9
                // - l'underscore
                // - le tiret
                if (IsAllowedCharacter(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Vérifie si un caractère est autorisé dans les noms générés par le Doc Generator.
        /// </summary>
        /// <param name="character">Caractère à vérifier.</param>
        /// <returns>
        /// True si le caractère est une lettre ASCII, un chiffre,
        /// un tiret ou un underscore.
        /// </returns>
        private static bool IsAllowedCharacter(char character)
        {
            bool isUppercaseLetter =
                character >= 'A'
                && character <= 'Z';

            bool isLowercaseLetter =
                character >= 'a'
                && character <= 'z';

            bool isDigit =
                character >= '0'
                && character <= '9';

            return isUppercaseLetter
                || isLowercaseLetter
                || isDigit
                || character == '_'
                || character == '-';
        }

        /// <summary>
        /// Supprime les signes diacritiques d'un texte.
        ///
        /// Les caractères accentués sont décomposés afin de séparer la lettre principale de son accent.
        ///
        /// Exemples :
        /// - é devient e
        /// - à devient a
        /// - ç devient c
        /// </summary>
        /// <param name="text">Texte contenant éventuellement des accents.</param>
        /// <returns>Texte équivalent sans signes diacritiques.</returns>
        private static string RemoveDiacritics(string text)
        {
            // Décompose les caractères accentués.
            // Exemple : é devient temporairement e + accent.
            string normalized = text.Normalize(
                NormalizationForm.FormD
            );

            StringBuilder builder = new StringBuilder();

            foreach (char character in normalized)
            {
                UnicodeCategory category =
                    CharUnicodeInfo.GetUnicodeCategory(character);

                // Les accents décomposés appartiennent à la catégorie NonSpacingMark et ne sont donc pas conservés.
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            // Recompose la chaîne après suppression des accents.
            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }
    }
}