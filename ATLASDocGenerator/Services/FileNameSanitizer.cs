using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ATLASDocGenerator.Services
{
    public static class FileNameSanitizer
    {
        public static string ToSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string cleaned = RemoveDiacritics(value.Trim());
            cleaned = cleaned.Replace(" ", "_");

            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder();

            foreach (char c in cleaned)
            {
                if (System.Array.IndexOf(invalidChars, c) >= 0)
                    continue;

                if (Regex.IsMatch(c.ToString(), @"[A-Za-z0-9_\-]"))
                    builder.Append(c);
            }

            return builder.ToString();
        }

        private static string RemoveDiacritics(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}