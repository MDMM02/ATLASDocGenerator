using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    /// <summary>
    /// Cette classe met à jour les variables générales du projet MadCap Flare
    /// après l'import d'un document Author-it.
    ///
    /// Elle modifie le fichier Project/VariableSets/General.flvar avec les informations
    /// renseignées dans la fenêtre AIT Import Finalizer :
    /// - type de document
    /// - nom du dispositif
    /// - référence et indice du document
    /// - langue
    /// - titre du document
    /// - référence MREF
    ///
    /// Une sauvegarde du fichier General.flvar est créée avant sa première modification.
    /// </summary>
    public class VariableSetUpdaterService
    {
        /// <summary>
        /// Met à jour les variables du fichier General.flvar.
        ///
        /// Traitement :
        /// 1. Vérifie que le chemin du projet est valide
        /// 2. Vérifie que le fichier General.flvar existe
        /// 3. Crée une sauvegarde du fichier
        /// 4. Charge le fichier comme document XML
        /// 5. Met à jour ou crée les variables attendues
        /// 6. Sauvegarde le fichier modifié
        /// </summary>
        /// <param name="projectRootPath">Chemin racine du projet MadCap Flare.</param>
        /// <param name="options">Options renseignées dans la fenêtre AIT Import Finalizer.</param>
        /// <param name="profile">
        /// Profil du type de document sélectionné.
        /// Ce paramètre n'est actuellement pas utilisé dans cette méthode.
        /// </param>
        public void UpdateGeneralVariables(
            string projectRootPath,
            AitImportFinalizerOptions options,
            AitDocumentProfile profile)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                throw new ArgumentException("Le chemin racine du projet est vide.", "projectRootPath");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (!Directory.Exists(projectRootPath))
            {
                throw new DirectoryNotFoundException(
                    "Dossier racine du projet introuvable : " + projectRootPath
                );
            }

            // Emplacement du fichier de variables générales dans le projet Flare.
            string variableSetPath = Path.Combine(
                projectRootPath,
                "Project",
                "VariableSets",
                "General.flvar"
            );

            if (!File.Exists(variableSetPath))
            {
                throw new FileNotFoundException(
                    "Le fichier de variables General.flvar est introuvable.",
                    variableSetPath
                );
            }

            // Crée une sauvegarde du fichier avant sa première modification.
            CreateBackup(variableSetPath);

            XDocument document = XDocument.Load(
                variableSetPath,
                LoadOptions.PreserveWhitespace
            );

            if (document.Root == null)
            {
                throw new InvalidOperationException(
                    "Le fichier General.flvar ne possède pas de racine XML."
                );
            }

            // Prépare les valeurs avant de les écrire dans le fichier.
            string guideType = GetGuideTypeLabel(options.DocumentType);
            string documentReference = Safe(options.DocumentReference);
            string documentIndex = Safe(options.DocumentIndex);

            string documentLanguage = string.IsNullOrWhiteSpace(options.Language)
                ? "FR"
                : options.Language.Trim();

            // Variables générales principales.
            SetVariable(document, "GuideType", guideType);
            SetVariable(document, "dispositif", Safe(options.DeviceName));
            SetVariable(document, "DocumentReference", documentReference);
            SetVariable(document, "Indice", documentIndex);
            SetVariable(document, "DocumentLanguage", documentLanguage);

            // Cette valeur est actuellement remise à 0 à chaque exécution.
            // À conserver uniquement si cette règle est bien souhaitée.
            SetVariable(document, "Version Interne", "0");

            // Variables utilisées par certains layouts ou snippets.
            SetVariable(document, "DocumentTitle", Safe(options.DocumentTitle));
            SetVariable(document, "TitreDocument", Safe(options.DocumentTitle));

            // Mref et MRef ne doivent pas être appelées séparément,
            // car la recherche des variables ne tient pas compte de la casse.
            SetVariable(document, "Mref", Safe(options.MrefReference));
            SetVariable(document, "ReferenceMref", Safe(options.MrefReference));

            // Sauvegarde sans reformater inutilement tout le document XML.
            document.Save(variableSetPath, SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Crée une copie de sauvegarde du fichier.
        ///
        /// Le backup est créé uniquement s'il n'existe pas déjà.
        /// Il représente donc l'état initial du fichier avant la première exécution.
        /// </summary>
        /// <param name="filePath">Chemin du fichier à sauvegarder.</param>
        private void CreateBackup(string filePath)
        {
            string backupPath = filePath + ".bak";

            if (!File.Exists(backupPath))
            {
                File.Copy(filePath, backupPath);
            }
        }

        /// <summary>
        /// Retourne le libellé du type de guide correspondant
        /// au type de document sélectionné.
        /// </summary>
        /// <param name="documentType">Type de document AIT.</param>
        /// <returns>Libellé à écrire dans la variable GuideType.</returns>
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

        /// <summary>
        /// Met à jour une variable existante ou la crée si elle n'existe pas.
        ///
        /// La recherche du nom de variable ne tient pas compte des majuscules.
        /// Par exemple, Mref et MRef sont considérées comme la même variable.
        /// </summary>
        /// <param name="document">Document XML représentant General.flvar.</param>
        /// <param name="variableName">Nom de la variable à mettre à jour.</param>
        /// <param name="value">Nouvelle valeur de la variable.</param>
        private void SetVariable(
            XDocument document,
            string variableName,
            string value)
        {
            XElement root = document.Root;

            XElement existingVariable = root
                .Descendants()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "Variable",
                        StringComparison.OrdinalIgnoreCase
                    )
                    && element.Attributes().Any(attribute =>
                        attribute.Name.LocalName.Equals(
                            "Name",
                            StringComparison.OrdinalIgnoreCase
                        )
                        && attribute.Value.Equals(
                            variableName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                );

            if (existingVariable != null)
            {
                existingVariable.Value = value ?? string.Empty;
                return;
            }

            // Utilise le même namespace que la racine du fichier.
            // Sans cela, la nouvelle variable pourrait être créée hors du namespace attendu.
            XNamespace ns = root.Name.Namespace;

            XElement newVariable = new XElement(ns + "Variable");
            newVariable.SetAttributeValue("Name", variableName);
            newVariable.Value = value ?? string.Empty;

            root.Add(newVariable);
        }

        /// <summary>
        /// Retourne une chaîne vide si la valeur est nulle.
        /// Sinon, supprime les espaces placés au début et à la fin.
        /// </summary>
        /// <param name="value">Valeur à sécuriser.</param>
        /// <returns>Valeur nettoyée ou chaîne vide.</returns>
        private string Safe(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }
    }
}