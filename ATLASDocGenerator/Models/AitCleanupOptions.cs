using System.Collections.Generic;

namespace ATLASDocGenerator.Models
{
    /// <summary>
    /// Regroupe toutes les options choisies par l'utilisateur
    /// dans la fenêtre AIT Cleanup.
    ///
    /// Cet objet sert uniquement à transmettre la configuration
    /// de nettoyage aux services de traitement.
    ///
    /// Il ne contient aucune logique métier.
    /// </summary>
    public class AitCleanupOptions
    {
        /// <summary>
        /// Initialise les collections utilisées par les traitements.
        ///
        /// Cela évite que SelectedIhmTemplateIds soit null
        /// lorsqu'aucun template IHM n'a été sélectionné.
        /// </summary>
        public AitCleanupOptions()
        {
            SelectedIhmTemplateIds = new List<string>();
        }

        /// <summary>
        /// Type de documentation traité.
        ///
        /// Exemples :
        /// - Doc user 
        /// - Doc tech
        /// </summary>
        public string DocumentationType { get; set; }

        /// <summary>
        /// Définit le périmètre du nettoyage :
        /// projet complet ou dossier sélectionné uniquement.
        /// </summary>
        public AitCleanupScope Scope { get; set; }

        /// <summary>
        /// Chemin du dossier, du dossier Content
        /// ou de la racine du projet Flare à traiter.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Chemin vers le fichier XML Author-it source.
        ///
        /// Ce fichier est utilisé pour :
        /// - détecter les templates Topic français ;
        /// - extraire les topics basés sur les templates sélectionnés ;
        /// - générer les fichiers de variables MadCap.
        /// </summary>
        public string SourceXmlPath { get; set; }

        /// <summary>
        /// Liste des IDs des templates IHM sélectionnés
        /// dans la fenêtre AIT Cleanup.
        ///
        /// Exemple :
        /// 18564 pour Menu_STR.
        ///
        /// Plusieurs templates peuvent être sélectionnés.
        /// </summary>
        public List<string> SelectedIhmTemplateIds { get; set; }

        /// <summary>
        /// Active la transformation des paragraphes
        /// Action / Résultat.
        /// </summary>
        public bool ProcessActionResults { get; set; }

        /// <summary>
        /// Active la transformation des paragraphes
        /// Author-it en listes à puces MadCap.
        /// </summary>
        public bool ProcessBulletLists { get; set; }

        /// <summary>
        /// Active la transformation des encadrés
        /// Information / Précaution / Attention.
        /// </summary>
        public bool ProcessCallouts { get; set; }

        /// <summary>
        /// Active le nettoyage ou la normalisation
        /// des figures importées depuis Author-it.
        /// </summary>
        public bool ProcessFigures { get; set; }

        /// <summary>
        /// Active le nettoyage des styles simples
        /// importés depuis Author-it.
        /// </summary>
        public bool ProcessStyleCleanup { get; set; }

        /// <summary>
        /// Active la détection des templates IHM
        /// et la génération des fichiers de variables.
        /// </summary>
        public bool ProcessIhm { get; set; }
    }
}