using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    public class VariableSetUpdaterService
    {
        public void UpdateGeneralVariables(string projectRootPath, AitImportFinalizerOptions options, AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                throw new ArgumentException("Project root path is empty.");
            }

            if (!Directory.Exists(projectRootPath))
            {
                throw new DirectoryNotFoundException("Project root folder not found: " + projectRootPath);
            }

            string variableSetPath = Path.Combine(projectRootPath, "Project", "VariableSets", "General.flvar");

            if (!File.Exists(variableSetPath))
            {
                throw new FileNotFoundException("General variable set not found.", variableSetPath);
            }

            CreateBackup(variableSetPath);

            XDocument document = XDocument.Load(variableSetPath, LoadOptions.PreserveWhitespace);

            if (document.Root == null)
            {
                throw new InvalidOperationException("General.flvar has no XML root.");
            }

            string guideType = GetGuideTypeLabel(options.DocumentType);
            string documentReference = Safe(options.DocumentReference);
            string documentIndex = Safe(options.DocumentIndex);
            string documentLanguage = string.IsNullOrWhiteSpace(options.Language) ? "FR" : options.Language.Trim();

            SetVariable(document, "GuideType", guideType);
            SetVariable(document, "dispositif", Safe(options.DeviceName));
            SetVariable(document, "DocumentReference", documentReference);
            SetVariable(document, "Indice", documentIndex);
            SetVariable(document, "DocumentLanguage", documentLanguage);
            SetVariable(document, "Version Interne", "0");

            // Variables utiles selon les layouts/snippets disponibles.
            SetVariable(document, "DocumentTitle", Safe(options.DocumentTitle));
            SetVariable(document, "TitreDocument", Safe(options.DocumentTitle));
            SetVariable(document, "Mref", Safe(options.MrefReference));
            SetVariable(document, "MRef", Safe(options.MrefReference));
            SetVariable(document, "ReferenceMref", Safe(options.MrefReference));

            document.Save(variableSetPath);
        }

        private void CreateBackup(string filePath)
        {
            string backupPath = filePath + ".bak";

            if (!File.Exists(backupPath))
            {
                File.Copy(filePath, backupPath);
            }
        }

        private string GetGuideTypeLabel(AitDocumentType documentType)
        {
            switch (documentType)
            {
                case AitDocumentType.TechnicalBulletin:
                    return "Bulletin Technique";

                case AitDocumentType.UserNotice:
                    return "Notice utilisateur";

                case AitDocumentType.Addenda:
                    return "Addenda";

                case AitDocumentType.ReferenceManual:
                    return "Manuel de référence";

                case AitDocumentType.MultiInstrumentTechnicalDocument:
                    return "Document technique multi-instrument";

                case AitDocumentType.TechnicalDocument:
                default:
                    return "Document technique";
            }
        }

        private void SetVariable(XDocument document, string variableName, string value)
        {
            XElement root = document.Root;

            XElement existingVariable = root
                .Descendants()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals("Variable", StringComparison.OrdinalIgnoreCase)
                    && element.Attributes().Any(attribute =>
                        attribute.Name.LocalName.Equals("Name", StringComparison.OrdinalIgnoreCase)
                        && attribute.Value.Equals(variableName, StringComparison.OrdinalIgnoreCase)));

            if (existingVariable != null)
            {
                existingVariable.Value = value ?? string.Empty;
                return;
            }

            XElement newVariable = new XElement("Variable");
            newVariable.SetAttributeValue("Name", variableName);
            newVariable.Value = value ?? string.Empty;

            root.Add(newVariable);
        }

        private string Safe(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }
    }
}