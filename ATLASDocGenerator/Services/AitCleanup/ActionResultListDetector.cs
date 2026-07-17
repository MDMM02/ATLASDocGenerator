using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATLASDocGenerator.Models;

namespace ATLASDocGenerator.Services.AitCleanup
{
    /// <summary>
    /// Cette classe detecte les paragraphes Action/Résultat importés.
    /// Parcourt les fichiers .htm et compte combien de paragraphes utilisent les classes a_action_num, a_action, a_action_b, a_result, a_result_b.
    /// La classe ne modifie pas les fichiers .htm, elle alimente le report avec des détails de diagnostic
    /// </summary>
    public class ActionResultListDetector
    {
        /// <summary>
        /// Analyse une liste de fichiers .htm pour détecter les paragraphes A/R, la méthode:
        /// 1. Charge le fichier .htm comme document XML
        /// 2. Parcours tous les paragraphes <p>
        /// 3.Compte les classes AIT liées aux A/R
        /// 4. Ajoute les compteurs dans le rapport
        /// 5. Ajoute une ligne de détails si des éléments sont détectés
        /// </summary>
        /// <param name="htmlFiles"></param> Liste de fichier .htm à analyser
        /// <param name="report"></param> Rapport de nettoyage à compléter
        /// <exception cref="ArgumentNullException"></exception>

        public void Detect(IEnumerable<string> htmlFiles, CleanupReport report)
        {
            if (htmlFiles == null)
            {
                throw new ArgumentNullException("htmlFiles");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            foreach (string filePath in htmlFiles)
            {
                try
                { // Charge le fichier .htm en conservant les espaces existants (important pr ne pas modifier le fichier).
                    XDocument document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                    int actionNumCount = 0; // Compte les paragraphes avec la classe a_action_num
                    int actionBulletCount = 0; // Compte les paragraphes avec la classe a_action ou a_action_b
                    int resultCount = 0; // Compte les paragraphes avec la classe a_resultat ou a_resultat_b

                    // Récupère tous les paragraphes <p>, peu importe le namespace XML
                    IEnumerable<XElement> paragraphs = document
                        .Descendants()
                        .Where(element => element.Name.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase));

                    foreach (XElement paragraph in paragraphs)
                    {
                        // Détection des actions numérotées
                        if (HasClass(paragraph, "a_action_num"))
                        {
                            actionNumCount++;
                        }
                        // Détection des actions sous forme standard ou bullet
                        if (HasClass(paragraph, "a_action") || HasClass(paragraph, "a_action_b"))
                        {
                            actionBulletCount++;
                        }
                        // Détection des paragraphes Résultat
                        if (HasClass(paragraph, "a_resultat") || HasClass(paragraph, "a_resultat_b"))
                        {
                            resultCount++;
                        }
                    }
                    // Si le fichier contient au moins un élément A/R on met à jour les compteurs globaux du rapport.
                    if (actionNumCount > 0 || actionBulletCount > 0 || resultCount > 0)
                    {
                        report.ActionNumParagraphsDetected += actionNumCount;
                        report.ActionBulletParagraphsDetected += actionBulletCount;
                        report.ResultParagraphsDetected += resultCount;

                        string detail =
                            Path.GetFileName(filePath)
                            + " | a_action_num: " + actionNumCount
                            + " | a_action/a_action_b: " + actionBulletCount
                            + " | a_resultat/a_resultat_b: " + resultCount;

                        report.ActionResultDetectionDetails.Add(detail);
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add("Action/result detection failed for file: " + filePath + " | " + ex.Message);
                }
            }
        }
        /// <summary>
        /// 
        /// Verifie si un élément XML possède une classe XML donnée.
        /// Le champ class peut contenir plusieurs classes séparées par des espaces.
        /// La méthode découpe l'attribut class avant de comparer les valeurs.
        /// 
        /// </summary>
        /// <param name="element"></param> Elément XML à analyser
        /// <param name="className"></param> Nom de la classe CSS recherchée
        /// <returns></returns> Retourne True si la classe est présente, sinon false.
        private bool HasClass(XElement element, string className)
        {
            XAttribute classAttribute = element.Attribute("class");

            if (classAttribute == null)
            {
                return false;
            }
            // Sépare les classes CSS même s'il y a des espaces, tabulations ou retours de ligne.
            string[] classes = classAttribute.Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return classes.Any(c => c.Equals(className, StringComparison.OrdinalIgnoreCase));
        }
    }
}