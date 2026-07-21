using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Cette classe duplique et configure une target MadCap Flare lors de la génération d'un nouveau document.
    ///
    /// Elle utilise la target modèle suivante : Project/Targets/Doc_SAV.fltar
    ///
    /// La target copiée est ensuite adaptée avec :
    /// - la nouvelle TOC du document
    /// - la feuille de style correspondant à la gamme
    /// - les variables propres au document
    /// - les conditions à exclure du build
    /// - la désactivation du glossaire et de l'index automatiques
    /// </summary>
    public class TargetDuplicator
    {
        // Namespace MadCap utilisé notamment pour les attributs MadCap:conditions.
        private static readonly XNamespace MadCapNs =
            "http://www.madcapsoftware.com/Schemas/MadCap.xsd";

        /// <summary>
        /// Duplique la target modèle puis met à jour son contenu.
        ///
        /// Traitement :
        /// 1. Vérifie que la target modèle existe
        /// 2. Vérifie qu'aucune target du même nom n'existe déjà
        /// 3. Copie la target modèle
        /// 4. Charge la copie comme document XML
        /// 5. Met à jour la TOC, la stylesheet, les variables et les conditions
        /// 6. Sauvegarde la nouvelle target
        /// 7. Retourne le chemin de la target créée
        /// </summary>
        /// <param name="projectRoot">Chemin racine du projet MadCap Flare.</param>
        /// <param name="folderName">Nom normalisé du dossier documentaire.</param>
        /// <param name="safeReference">Référence normalisée du document.</param>
        /// <param name="range">Gamme sélectionnée par l'utilisateur.</param>
        /// <param name="device">Nom du dispositif.</param>
        /// <param name="fullTitle">Titre complet du document.</param>
        /// <returns>Chemin complet de la target créée.</returns>
        public string DuplicateAndUpdateTarget(
            string projectRoot,
            string folderName,
            string safeReference,
            string range,
            string device,
            string fullTitle)
        {
            string sourceTargetPath = Path.Combine(
                projectRoot,
                "Project",
                "Targets",
                "Doc_SAV.fltar"
            );

            string targetTargetPath = Path.Combine(
                projectRoot,
                "Project",
                "Targets",
                folderName + ".fltar"
            );

            if (!File.Exists(sourceTargetPath))
            {
                throw new Exception(
                    "Target modèle introuvable :\n"
                    + sourceTargetPath
                );
            }

            if (File.Exists(targetTargetPath))
            {
                throw new Exception(
                    "Une target existe déjà avec ce nom :\n"
                    + targetTargetPath
                );
            }

            // Copie la target modèle sans écraser une target existante.
            File.Copy(
                sourceTargetPath,
                targetTargetPath
            );

            XDocument document;

            try
            {
                // Charge la target copiée comme document XML en conservant les espaces existants.
                document = XDocument.Load(
                    targetTargetPath,
                    LoadOptions.PreserveWhitespace
                );
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Impossible de lire la target copiée comme XML :\n"
                    + targetTargetPath
                    + "\n\nDétail : "
                    + ex.Message
                );
            }

            // Applique les paramètres du nouveau document.
            UpdateTarget(
                document,
                folderName,
                safeReference,
                range,
                device,
                fullTitle
            );

            // Sauvegarde sans reformater inutilement la target.
            document.Save(
                targetTargetPath,
                SaveOptions.DisableFormatting
            );

            return targetTargetPath;
        }

        /// <summary>
        /// Met à jour le contenu XML de la target dupliquée.
        ///
        /// Cette méthode :
        /// - retire certaines conditions présentes dans les attributs XML
        /// - ajoute les conditions à exclure du build
        /// - désactive la génération automatique de l'index et du glossaire
        /// - configure la TOC principale
        /// - configure la feuille de style principale
        /// - ajoute ou met à jour les variables du document
        /// </summary>
        /// <param name="document">Document XML représentant la target.</param>
        /// <param name="folderName">Nom du dossier documentaire.</param>
        /// <param name="safeReference">Référence normalisée du document.</param>
        /// <param name="range">Gamme du document.</param>
        /// <param name="device">Nom du dispositif.</param>
        /// <param name="fullTitle">Titre complet du document.</param>
        private void UpdateTarget(
            XDocument document,
            string folderName,
            string safeReference,
            string range,
            string device,
            string fullTitle)
        {
            if (document.Root == null)
            {
                throw new Exception(
                    "La target ne contient pas d'élément racine."
                );
            }

            // Retire les conditions qui ne doivent plus être portées directement par les éléments ou attributs de la target.
            RemoveTargetConditions(document);

            // Ajoute ces mêmes conditions dans la liste des conditions explicitement exclues du build.
            EnsureExcludedConditions(document);

            // Désactive l'index et le glossaire générés automatiquement.
            DisableAutoGeneratedBackMatter(document);

            // Configure la TOC créée pour le nouveau document.
            document.Root.SetAttributeValue(
                "MasterToc",
                "/Project/TOCs/" + folderName + ".fltoc"
            );

            // Configure la feuille de style selon la gamme sélectionnée.
            document.Root.SetAttributeValue(
                "MasterStylesheet",
                GetStylesheetPath(range)
            );

            // Recherche le bloc Variables existant directement sous la racine.
            XElement variablesElement = document.Root
                .Elements()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "Variables",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (variablesElement == null)
            {
                // Utilise le même namespace XML que la racine de la target.
                variablesElement = new XElement(
                    document.Root.Name.Namespace + "Variables"
                );

                document.Root.Add(variablesElement);
            }

            // Variables propres au nouveau document.
            SetOrCreateVariable(
                variablesElement,
                "General/dispositif",
                device
            );

            /*
             * Point à vérifier :
             * GuideType représente normalement le type de guide et non le titre complet du document.
             *
             * Il faudra vérifier si cette variable doit plutôt être :
             * General/DocumentTitle ou General/TitreDocument.
             *
             * La logique actuelle est conservée pour ne pas modifier le comportement existant du plugin.
             */
            SetOrCreateVariable(
                variablesElement,
                "General/GuideType",
                fullTitle
            );

            SetOrCreateVariable(
                variablesElement,
                "General/DocumentReference",
                safeReference
            );
        }

        /// <summary>
        /// Met à jour une variable de target ou la crée si elle n'existe pas.
        ///
        /// Les variables sont stockées dans le bloc Variables de la target.
        /// La recherche du nom ne tient pas compte des majuscules.
        /// </summary>
        /// <param name="variablesElement">Bloc Variables de la target.</param>
        /// <param name="variableName">Nom complet de la variable.</param>
        /// <param name="value">Valeur à appliquer.</param>
        private void SetOrCreateVariable(
            XElement variablesElement,
            string variableName,
            string value)
        {
            XElement variable = variablesElement
                .Elements()
                .FirstOrDefault(element =>
                {
                    if (!element.Name.LocalName.Equals(
                        "Variable",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    XAttribute nameAttribute = element
                        .Attributes()
                        .FirstOrDefault(attribute =>
                            attribute.Name.LocalName.Equals(
                                "Name",
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                    return nameAttribute != null
                        && nameAttribute.Value.Equals(
                            variableName,
                            StringComparison.OrdinalIgnoreCase
                        );
                });

            if (variable == null)
            {
                variable = new XElement(
                    variablesElement.Name.Namespace + "Variable"
                );

                variable.SetAttributeValue(
                    "Name",
                    variableName
                );

                variablesElement.Add(variable);
            }

            variable.Value = value ?? string.Empty;
        }

        /// <summary>
        /// Retire certaines conditions des attributs conditions présents dans la target.
        ///
        /// La méthode traite :
        /// - l'attribut conditions sans namespace
        /// - l'attribut MadCap:conditions
        /// </summary>
        /// <param name="document">Document XML représentant la target.</param>
        private void RemoveTargetConditions(XDocument document)
        {
            if (document.Root == null)
            {
                return;
            }

            // Parcourt la racine puis tous ses descendants.
            // Cette écriture évite l'utilisation de DescendantsAndSelf(), qui avait déjà causé une erreur de compatibilité.
            foreach (XElement element in new[] { document.Root }
                .Concat(document.Root.Descendants()))
            {
                RemoveConditionsFromAttribute(
                    element,
                    "conditions"
                );

                RemoveConditionsFromAttribute(
                    element,
                    MadCapNs + "conditions"
                );
            }
        }

        /// <summary>
        /// Retire les conditions ATLAS ciblées d'un attribut XML.
        ///
        /// Les autres conditions présentes dans l'attribut sont conservées.
        /// Si aucune condition ne reste, l'attribut est supprimé.
        /// </summary>
        /// <param name="element">Élément XML à nettoyer.</param>
        /// <param name="attributeName">Nom de l'attribut conditions.</param>
        private void RemoveConditionsFromAttribute(
            XElement element,
            XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return;
            }

            string[] conditionsToRemove =
            {
                "Stago_Gestion.Contenu commun",
                "Stago_Gestion.Commun_Tech",
                "Stago_Gestion.40_DoNotTranslate",
                "Stago_Gestion.Commentaires"
            };

            string[] remainingConditions = attribute.Value
                .Split(',')
                .Select(condition => condition.Trim())
                .Where(condition =>
                    !conditionsToRemove.Any(conditionToRemove =>
                        condition.Equals(
                            conditionToRemove,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(condition =>
                    !string.IsNullOrWhiteSpace(condition)
                )
                .ToArray();

            if (remainingConditions.Length == 0)
            {
                attribute.Remove();
            }
            else
            {
                attribute.Value = string.Join(
                    ",",
                    remainingConditions
                );
            }
        }

        /// <summary>
        /// Vérifie que les conditions ATLAS attendues sont bien configurées avec l'action Exclude.
        ///
        /// Si le bloc ConditionTagExpression n'existe pas, il est créé dans la target.
        /// </summary>
        /// <param name="document">Document XML représentant la target.</param>
        private void EnsureExcludedConditions(XDocument document)
        {
            if (document.Root == null)
            {
                return;
            }

            XElement conditionTagExpression = document.Root
                .Elements()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "ConditionTagExpression",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (conditionTagExpression == null)
            {
                conditionTagExpression = new XElement(
                    document.Root.Name.Namespace
                    + "ConditionTagExpression"
                );

                document.Root.Add(conditionTagExpression);
            }

            AddExcludeCondition(
                conditionTagExpression,
                "Stago_Gestion.Commentaires"
            );

            AddExcludeCondition(
                conditionTagExpression,
                "Stago_Gestion.Contenu commun"
            );

            AddExcludeCondition(
                conditionTagExpression,
                "Stago_Gestion.Commun_Tech"
            );

            AddExcludeCondition(
                conditionTagExpression,
                "Stago_Gestion.40_DoNotTranslate"
            );
        }

        /// <summary>
        /// Ajoute une condition avec l'action Exclude uniquement si elle n'existe pas déjà.
        /// </summary>
        /// <param name="conditionTagExpression">
        /// Bloc ConditionTagExpression de la target.
        /// </param>
        /// <param name="condition">Nom complet de la condition.</param>
        private void AddExcludeCondition(
            XElement conditionTagExpression,
            string condition)
        {
            bool alreadyExists = conditionTagExpression
                .Elements()
                .Any(element =>
                {
                    if (!element.Name.LocalName.Equals(
                        "Tag",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    XAttribute nameAttribute = element
                        .Attributes()
                        .FirstOrDefault(attribute =>
                            attribute.Name.LocalName.Equals(
                                "Name",
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                    XAttribute actionAttribute = element
                        .Attributes()
                        .FirstOrDefault(attribute =>
                            attribute.Name.LocalName.Equals(
                                "Action",
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                    return nameAttribute != null
                        && actionAttribute != null
                        && nameAttribute.Value.Equals(
                            condition,
                            StringComparison.OrdinalIgnoreCase
                        )
                        && actionAttribute.Value.Equals(
                            "Exclude",
                            StringComparison.OrdinalIgnoreCase
                        );
                });

            if (alreadyExists)
            {
                return;
            }

            XElement tag = new XElement(
                conditionTagExpression.Name.Namespace + "Tag"
            );

            tag.SetAttributeValue(
                "Name",
                condition
            );

            tag.SetAttributeValue(
                "Action",
                "Exclude"
            );

            conditionTagExpression.Add(tag);
        }

        /// <summary>
        /// Désactive la génération automatique du glossaire et de l'index dans la sortie imprimée.
        ///
        /// Si le bloc PrintedOutput n'existe pas, aucune modification n'est appliquée.
        /// </summary>
        /// <param name="document">Document XML représentant la target.</param>
        private void DisableAutoGeneratedBackMatter(XDocument document)
        {
            if (document.Root == null)
            {
                return;
            }

            XElement printedOutput = document.Root
                .Elements()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "PrintedOutput",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (printedOutput == null)
            {
                return;
            }

            printedOutput.SetAttributeValue(
                "GenerateGlossaryProxy",
                "false"
            );

            printedOutput.SetAttributeValue(
                "GenerateIndexProxy",
                "false"
            );
        }

        /// <summary>
        /// Retourne le chemin de la feuille de style selon la gamme sélectionnée.
        ///
        /// La gamme STA utilise Styles_STA.css.
        /// Toutes les autres gammes utilisent Styles.css.
        /// </summary>
        /// <param name="range">Gamme sélectionnée.</param>
        /// <returns>Chemin de la feuille de style.</returns>
        private string GetStylesheetPath(string range)
        {
            if (string.Equals(
                range,
                "STA",
                StringComparison.OrdinalIgnoreCase))
            {
                return "/Content/Resources/Stylesheets/Styles_STA.css";
            }

            return "/Content/Resources/Stylesheets/Styles.css";
        }
    }
}