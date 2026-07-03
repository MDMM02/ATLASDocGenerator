using ATLASDocGenerator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services.AitCleanup
{
    public class IhmVariableMatcher
    {
        private const string SourceClassName = "a_noir_gras_char";
        private const string DefaultTargetClassName = "a_gras_car";
        private const string IhmTargetClassName = "IHM";

        public void Transform(List<string> files, CleanupReport report, string flareProjectRootPath)
        {
            if (files == null || files.Count == 0)
                return;

            if (report == null)
                return;

            HashSet<string> ihmValues = LoadIhmValues(flareProjectRootPath, report);

            if (ihmValues == null || ihmValues.Count == 0)
            {
                report.Warnings.Add("IHM variable matching skipped: no IHM values were found in Project\\ATLAS\\IhmVariables.json.");
                return;
            }

            foreach (string filePath in files)
            {
                TransformFile(filePath, report, ihmValues);
            }
        }

        private void TransformFile(string filePath, CleanupReport report, HashSet<string> ihmValues)
        {
            try
            {
                XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                int ihmTransformed = 0;
                int defaultMapped = 0;

                IEnumerable<XElement> spans = document
                    .Descendants()
                    .Where(e =>
                        string.Equals(e.Name.LocalName, "span", StringComparison.OrdinalIgnoreCase)
                        && HasClass(e, SourceClassName))
                    .ToList();

                foreach (XElement span in spans)
                {
                    string text = NormalizeText(span.Value);

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    if (ihmValues.Contains(text))
                    {
                        ReplaceClass(span, SourceClassName, IhmTargetClassName);
                        ihmTransformed++;
                    }
                    else
                    {
                        ReplaceClass(span, SourceClassName, DefaultTargetClassName);
                        defaultMapped++;
                    }
                }

                if (ihmTransformed > 0 || defaultMapped > 0)
                {
                    document.Save(filePath);

                    report.IhmItemsDetected += ihmTransformed;

                    report.Warnings.Add(
                        Path.GetFileName(filePath)
                        + " | IHM matched: "
                        + ihmTransformed
                        + " | a_noir_gras_char mapped to a_gras_car: "
                        + defaultMapped);
                }
            }
            catch (Exception ex)
            {
                report.Warnings.Add("IHM variable matching skipped file: " + filePath + " - " + ex.Message);
            }
        }

        private HashSet<string> LoadIhmValues(string flareProjectRootPath, CleanupReport report)
        {
            if (string.IsNullOrWhiteSpace(flareProjectRootPath))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string jsonPath = Path.Combine(flareProjectRootPath, "Project", "ATLAS", "IhmVariables.json");

            if (!File.Exists(jsonPath))
            {
                report.Warnings.Add("IHM variable JSON not found: " + jsonPath);
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            string json = File.ReadAllText(jsonPath);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            IhmVariableJsonModel model = serializer.Deserialize<IhmVariableJsonModel>(json);

            HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (model == null || model.Items == null)
                return values;

            foreach (IhmVariableJsonItem item in model.Items)
            {
                if (item == null)
                    continue;

                string text = NormalizeText(item.Text);

                if (!string.IsNullOrWhiteSpace(text))
                    values.Add(text);
            }

            return values;
        }

        private bool HasClass(XElement element, string className)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
                return false;

            string[] classes = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return classes.Any(c => string.Equals(c, className, StringComparison.OrdinalIgnoreCase));
        }

        private void ReplaceClass(XElement element, string oldClassName, string newClassName)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
                return;

            List<string> classes = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            for (int i = 0; i < classes.Count; i++)
            {
                if (string.Equals(classes[i], oldClassName, StringComparison.OrdinalIgnoreCase))
                    classes[i] = newClassName;
            }

            classAttribute.Value = string.Join(" ", classes.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
        }
    }
}