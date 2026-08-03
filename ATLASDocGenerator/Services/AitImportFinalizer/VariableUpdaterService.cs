using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services;

namespace ATLASDocGenerator.Services.AitImportFinalizer
{
    /// <summary>
    /// Cette classe met à jour les variables générales du projet Flare.
    ///
    /// Elle modifie le fichier Project/VariableSets/General.flvar
    /// avec les informations renseignées dans la fenêtre AIT Import Finalizer :
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
        public void UpdateGeneralVariables(
            string projectRootPath,
            AitImportFinalizerOptions options)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                throw new ArgumentException(
                    "Le chemin racine du projet est vide.",
                    "projectRootPath"
                );
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (!Directory.Exists(projectRootPath))
            {
                throw new DirectoryNotFoundException(
                    "Dossier racine du projet introuvable : "
                    + projectRootPath
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

            // Charge le fichier comme document XML
            // en conservant les espaces et retours à la ligne existants.
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
            SetVariable(
                document,
                "GuideType",
                guideType
            );

            SetVariable(
                document,
                "dispositif",
                Safe(options.DeviceName)
            );

            SetVariable(
                document,
                "DocumentReference",
                documentReference
            );

            SetVariable(
                document,
                "Indice",
                documentIndex
            );

            SetVariable(
                document,
                "DocumentLanguage",
                documentLanguage
            );

            // Cette valeur est actuellement remise à 0 à chaque exécution.
            // À conserver uniquement si cette règle est bien souhaitée.
            SetVariable(
                document,
                "Version Interne",
                "0"
            );

            // Variables utilisées par certains layouts ou snippets.
            SetVariable(
                document,
                "DocumentTitle",
                Safe(options.DocumentTitle)
            );

            SetVariable(
                document,
                "TitreDocument",
                Safe(options.DocumentTitle)
            );

            // Mref et MRef ne sont pas appelées séparément,
            // car la recherche ne tient pas compte des majuscules.
            SetVariable(
                document,
                "Mref",
                Safe(options.MrefReference)
            );

            SetVariable(
                document,
                "ReferenceMref",
                Safe(options.MrefReference)
            );

            // La sauvegarde est créée seulement après validation et préparation
            // complète du document, immédiatement avant la première écriture.
            FileBackupService.CreateInitialBackup(
                variableSetPath,
                ".bak");

            // Sauvegarde sans reformater inutilement tout le document XML.
            document.Save(
                variableSetPath,
                SaveOptions.DisableFormatting
            );
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
        /// La méthode prend en charge deux structures possibles :
        ///
        /// Structure MadCap avec VariableDefinition :
        /// Variable
        ///     VariableDefinition
        ///
        /// Ou structure simple avec une valeur directement
        /// contenue dans l'élément Variable.
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

            if (root == null)
            {
                throw new InvalidOperationException(
                    "Le fichier General.flvar ne possède pas de racine XML."
                );
            }

            string safeValue = value ?? string.Empty;

            // Recherche une variable existante avec le même nom.
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
                // Recherche une structure MadCap de ce type :
                //
                // <Variable Name="DocumentTitle">
                //     <VariableDefinition>Ancien titre</VariableDefinition>
                // </Variable>
                XElement variableDefinition = existingVariable
                    .Elements()
                    .FirstOrDefault(element =>
                        element.Name.LocalName.Equals(
                            "VariableDefinition",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (variableDefinition != null)
                {
                    // Met uniquement à jour la valeur
                    // sans supprimer l'élément VariableDefinition.
                    variableDefinition.Value = safeValue;
                }
                else
                {
                    // Fallback pour une structure simple :
                    //
                    // <Variable Name="DocumentTitle">Ancien titre</Variable>
                    existingVariable.Value = safeValue;
                }

                return;
            }

            // La variable n'existe pas encore.
            // On récupère le namespace utilisé par les variables existantes.
            XElement existingVariableExample = root
                .Descendants()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "Variable",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            XNamespace variableNamespace = existingVariableExample != null
                ? existingVariableExample.Name.Namespace
                : root.Name.Namespace;

            XElement newVariable = new XElement(
                variableNamespace + "Variable"
            );

            newVariable.SetAttributeValue(
                "Name",
                variableName
            );

            // Vérifie si le fichier utilise déjà des éléments VariableDefinition.
            XElement existingDefinitionExample = root
                .Descendants()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "VariableDefinition",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (existingDefinitionExample != null)
            {
                // Reproduit la structure déjà utilisée dans General.flvar.
                XNamespace definitionNamespace =
                    existingDefinitionExample.Name.Namespace;

                XElement newDefinition = new XElement(
                    definitionNamespace + "VariableDefinition",
                    safeValue
                );

                newVariable.Add(newDefinition);
            }
            else
            {
                // Fallback si le fichier utilise une structure simple.
                newVariable.Value = safeValue;
            }

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
            return value == null
                ? string.Empty
                : value.Trim();
        }
    }
}
