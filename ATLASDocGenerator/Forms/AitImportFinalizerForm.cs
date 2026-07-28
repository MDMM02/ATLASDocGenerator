using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitImportFinalizer;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ATLASDocGenerator.Forms
{
    /// <summary>
    /// Fenêtre permettant de finaliser un projet MadCap Flare après un import depuis Author-it.
    ///
    /// Le Finalizer peut :
    /// - installer les ressources ATLAS ;
    /// - nettoyer la TOC sélectionnée ;
    /// - mettre à jour le fichier General.flvar ;
    /// - configurer la TOC, la stylesheet et le layout de la target.
    /// </summary>
    public class AitImportFinalizerForm : Form
    {
        private ComboBox documentTypeComboBox;

        private TextBox titleTextBox;
        private TextBox deviceTextBox;
        private TextBox referenceTextBox;
        private TextBox indexTextBox;
        private TextBox languageTextBox;
        private TextBox mrefTextBox;

        private TextBox tocPathTextBox;
        private Button browseTocButton;

        private TextBox targetPathTextBox;
        private Button browseTargetButton;

        private CheckBox copyResourcesCheckBox;
        private CheckBox cleanTocCheckBox;
        private CheckBox updateVariablesCheckBox;
        private CheckBox configureTargetCheckBox;

        private Button runButton;
        private Button cancelButton;

        public AitImportFinalizerForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Construit l'interface du Finalizer.
        /// </summary>
        private void InitializeComponent()
        {
            Text = "AIT Import Finalizer";
            Width = 720;
            Height = 850;
            MinimumSize = new System.Drawing.Size(720, 700);

            AutoScroll = true;
            StartPosition = FormStartPosition.CenterParent;
            Font = new System.Drawing.Font(
                "Segoe UI",
                9,
                System.Drawing.FontStyle.Regular);

            Label titleLabel = new Label
            {
                Text = "Finaliser un import Author-it vers MadCap",
                Left = 20,
                Top = 18,
                Width = 650,
                Height = 25,
                Font = new System.Drawing.Font(
                    "Segoe UI",
                    11,
                    System.Drawing.FontStyle.Bold)
            };

            Label introductionLabel = new Label
            {
                Text =
                    "Sélectionnez les fichiers du projet, renseignez les informations "
                    + "du document, puis choisissez les actions à exécuter.",
                Left = 20,
                Top = 47,
                Width = 650,
                Height = 35
            };

            Controls.Add(titleLabel);
            Controls.Add(introductionLabel);

            int top = 90;

            /*
             * SECTION 1
             * Fichiers du projet.
             */

            AddSectionTitle(
                "1. Fichiers du projet",
                top);

            top += 35;

            Label documentTypeLabel = new Label
            {
                Text = "Type de document :",
                Left = 20,
                Top = top + 4,
                Width = 170
            };

            documentTypeComboBox = new ComboBox
            {
                Left = 200,
                Top = top,
                Width = 440,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            documentTypeComboBox.Items.Add(
                new ComboItem(
                    "Bulletin Technique",
                    AitDocumentType.TechnicalBulletin));

            documentTypeComboBox.Items.Add(
                new ComboItem(
                    "Notice utilisateur",
                    AitDocumentType.UserNotice));

            documentTypeComboBox.Items.Add(
                new ComboItem(
                    "Addenda",
                    AitDocumentType.Addenda));

            documentTypeComboBox.Items.Add(
                new ComboItem(
                    "Manuel de référence / MRef",
                    AitDocumentType.ReferenceManual));

            documentTypeComboBox.Items.Add(
                new ComboItem(
                    "Document technique",
                    AitDocumentType.TechnicalDocument));

            documentTypeComboBox.Items.Add(
                new ComboItem(
                    "Document technique multi-instrument",
                    AitDocumentType.MultiInstrumentTechnicalDocument));

            documentTypeComboBox.SelectedIndex = 0;

            Controls.Add(documentTypeLabel);
            Controls.Add(documentTypeComboBox);

            top += 40;

            Label tocLabel = new Label
            {
                Text = "TOC MadCap :",
                Left = 20,
                Top = top + 4,
                Width = 170
            };

            tocPathTextBox = new TextBox
            {
                Left = 200,
                Top = top,
                Width = 340
            };

            browseTocButton = new Button
            {
                Text = "Parcourir",
                Left = 550,
                Top = top - 1,
                Width = 90
            };

            browseTocButton.Click += BrowseTocButton_Click;

            Controls.Add(tocLabel);
            Controls.Add(tocPathTextBox);
            Controls.Add(browseTocButton);

            top += 31;

            AddHelpLabel(
                "La TOC permet également d'identifier automatiquement "
                + "le dossier racine du projet Flare.",
                top);

            top += 43;

            Label targetLabel = new Label
            {
                Text = "Target PDF MadCap :",
                Left = 20,
                Top = top + 4,
                Width = 170
            };

            targetPathTextBox = new TextBox
            {
                Left = 200,
                Top = top,
                Width = 340
            };

            browseTargetButton = new Button
            {
                Text = "Parcourir",
                Left = 550,
                Top = top - 1,
                Width = 90
            };

            browseTargetButton.Click += BrowseTargetButton_Click;

            Controls.Add(targetLabel);
            Controls.Add(targetPathTextBox);
            Controls.Add(browseTargetButton);

            top += 50;

            /*
             * SECTION 2
             * Informations écrites dans General.flvar.
             */

            AddSectionTitle(
                "2. Informations du document",
                top);

            top += 31;

            AddHelpLabel(
                "Ces informations sont écrites dans "
                + "Project/VariableSets/General.flvar uniquement lorsque "
                + "l'action « Mettre à jour General.flvar » est sélectionnée.",
                top);

            top += 48;

            titleTextBox = AddTextField(
                "Titre document :",
                top);

            top += 35;

            deviceTextBox = AddTextField(
                "Dispositif :",
                top);

            top += 35;

            referenceTextBox = AddTextField(
                "Référence :",
                top);

            top += 35;

            indexTextBox = AddTextField(
                "Indice :",
                top);

            top += 35;

            languageTextBox = AddTextField(
                "Langue :",
                top);

            languageTextBox.Text = "FR";

            top += 35;

            mrefTextBox = AddTextField(
                "Réf. MRef :",
                top);

            top += 50;

            /*
             * SECTION 3
             * Actions disponibles.
             */

            AddSectionTitle(
                "3. Actions de finalisation",
                top);

            top += 35;

            copyResourcesCheckBox =
                AddCheckBoxWithDescription(
                    "Installer ou mettre à jour les ressources ATLAS",
                    "Copie les stylesheets, layouts, images, snippets, "
                    + "variables et ressources Commun Stago dans le projet.",
                    top,
                    true);

            top += 59;

            cleanTocCheckBox =
                AddCheckBoxWithDescription(
                    "Nettoyer la TOC sélectionnée",
                    "Supprime les entrées parasites définies dans le profil "
                    + "du type de document sélectionné.",
                    top,
                    true);

            top += 59;

            updateVariablesCheckBox =
                AddCheckBoxWithDescription(
                    "Mettre à jour General.flvar",
                    "Écrit le titre, le dispositif, la référence, l'indice, "
                    + "la langue et la référence MRef.",
                    top,
                    true);

            updateVariablesCheckBox.CheckedChanged +=
                UpdateControlStates_CheckedChanged;

            top += 59;

            configureTargetCheckBox =
                AddCheckBoxWithDescription(
                    "Configurer la TOC, le style et le layout de la target",
                    "Associe la TOC sélectionnée, la feuille de style primaire "
                    + "et la mise en page primaire à la target.",
                    top,
                    true);

            configureTargetCheckBox.CheckedChanged +=
                UpdateControlStates_CheckedChanged;

            top += 73;

            runButton = new Button
            {
                Text = "Finaliser l'import",
                Left = 390,
                Top = top,
                Width = 145,
                Height = 32
            };

            runButton.Click += RunButton_Click;

            cancelButton = new Button
            {
                Text = "Annuler",
                Left = 545,
                Top = top,
                Width = 95,
                Height = 32,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(runButton);
            Controls.Add(cancelButton);

            AcceptButton = runButton;
            CancelButton = cancelButton;

            UpdateControlStates();
        }

        /// <summary>
        /// Ajoute un titre de section dans la fenêtre.
        /// </summary>
        private void AddSectionTitle(
            string text,
            int top)
        {
            Label sectionLabel = new Label
            {
                Text = text,
                Left = 20,
                Top = top,
                Width = 650,
                Height = 24,
                Font = new System.Drawing.Font(
                    "Segoe UI",
                    9,
                    System.Drawing.FontStyle.Bold)
            };

            Controls.Add(sectionLabel);
        }

        /// <summary>
        /// Ajoute un texte explicatif sous un champ ou une section.
        /// </summary>
        private void AddHelpLabel(
            string text,
            int top)
        {
            Label helpLabel = new Label
            {
                Text = text,
                Left = 200,
                Top = top,
                Width = 440,
                Height = 38,
                ForeColor = System.Drawing.SystemColors.GrayText
            };

            Controls.Add(helpLabel);
        }

        /// <summary>
        /// Ajoute un champ texte avec son libellé.
        /// </summary>
        private TextBox AddTextField(
            string label,
            int top)
        {
            Label fieldLabel = new Label
            {
                Text = label,
                Left = 20,
                Top = top + 4,
                Width = 170
            };

            TextBox textBox = new TextBox
            {
                Left = 200,
                Top = top,
                Width = 440
            };

            Controls.Add(fieldLabel);
            Controls.Add(textBox);

            return textBox;
        }

        /// <summary>
        /// Ajoute une action avec une description de son effet.
        /// </summary>
        private CheckBox AddCheckBoxWithDescription(
            string text,
            string description,
            int top,
            bool isChecked)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text,
                Left = 200,
                Top = top,
                Width = 440,
                Height = 23,
                Checked = isChecked
            };

            Label descriptionLabel = new Label
            {
                Text = description,
                Left = 220,
                Top = top + 24,
                Width = 420,
                Height = 34,
                ForeColor = System.Drawing.SystemColors.GrayText
            };

            Controls.Add(checkBox);
            Controls.Add(descriptionLabel);

            return checkBox;
        }

        /// <summary>
        /// Active ou désactive les champs selon les actions cochées.
        /// </summary>
        private void UpdateControlStates_CheckedChanged(
            object sender,
            EventArgs e)
        {
            UpdateControlStates();
        }

        /// <summary>
        /// Les champs documentaires ne sont utiles que pour General.flvar.
        /// Le chemin de target n'est utile que pour la configuration de target.
        /// </summary>
        private void UpdateControlStates()
        {
            bool variablesEnabled =
                updateVariablesCheckBox != null
                && updateVariablesCheckBox.Checked;

            titleTextBox.Enabled = variablesEnabled;
            deviceTextBox.Enabled = variablesEnabled;
            referenceTextBox.Enabled = variablesEnabled;
            indexTextBox.Enabled = variablesEnabled;
            languageTextBox.Enabled = variablesEnabled;
            mrefTextBox.Enabled = variablesEnabled;

            bool targetEnabled =
                configureTargetCheckBox != null
                && configureTargetCheckBox.Checked;

            targetPathTextBox.Enabled = targetEnabled;
            browseTargetButton.Enabled = targetEnabled;
        }

        /// <summary>
        /// Ouvre la boîte de dialogue permettant de sélectionner une TOC.
        /// </summary>
        private void BrowseTocButton_Click(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title =
                    "Sélectionner la TOC MadCap";

                dialog.Filter =
                    "MadCap TOC (*.fltoc)|*.fltoc|"
                    + "Tous les fichiers (*.*)|*.*";

                if (dialog.ShowDialog(this) ==
                    DialogResult.OK)
                {
                    tocPathTextBox.Text =
                        dialog.FileName;
                }
            }
        }

        /// <summary>
        /// Ouvre la boîte de dialogue permettant de sélectionner une target.
        /// </summary>
        private void BrowseTargetButton_Click(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title =
                    "Sélectionner la target PDF MadCap";

                dialog.Filter =
                    "MadCap Target (*.fltar)|*.fltar|"
                    + "Tous les fichiers (*.*)|*.*";

                if (dialog.ShowDialog(this) ==
                    DialogResult.OK)
                {
                    targetPathTextBox.Text =
                        dialog.FileName;
                }
            }
        }

        /// <summary>
        /// Valide les informations puis lance le Finalizer.
        /// </summary>
        private void RunButton_Click(
            object sender,
            EventArgs e)
        {
            ComboItem selectedItem =
                documentTypeComboBox.SelectedItem
                as ComboItem;

            if (selectedItem == null)
            {
                ShowValidationMessage(
                    "Veuillez sélectionner un type de document.");

                return;
            }

            if (!copyResourcesCheckBox.Checked
                && !cleanTocCheckBox.Checked
                && !updateVariablesCheckBox.Checked
                && !configureTargetCheckBox.Checked)
            {
                ShowValidationMessage(
                    "Veuillez sélectionner au moins une action "
                    + "de finalisation.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                tocPathTextBox.Text))
            {
                ShowValidationMessage(
                    "Veuillez sélectionner la TOC du projet.");

                return;
            }

            if (!File.Exists(
                tocPathTextBox.Text))
            {
                ShowValidationMessage(
                    "La TOC sélectionnée est introuvable.");

                return;
            }

            string projectRootPath =
                GetProjectRootPathFromToc(
                    tocPathTextBox.Text);

            if (string.IsNullOrWhiteSpace(
                projectRootPath))
            {
                ShowValidationMessage(
                    "Impossible d'identifier le dossier racine "
                    + "du projet à partir de la TOC sélectionnée.");

                return;
            }

            if (configureTargetCheckBox.Checked)
            {
                if (string.IsNullOrWhiteSpace(
                    targetPathTextBox.Text))
                {
                    ShowValidationMessage(
                        "Veuillez sélectionner la target PDF "
                        + "à configurer.");

                    return;
                }

                if (!File.Exists(
                    targetPathTextBox.Text))
                {
                    ShowValidationMessage(
                        "La target PDF sélectionnée est introuvable.");

                    return;
                }
            }

            AitImportFinalizerOptions options =
                new AitImportFinalizerOptions
                {
                    DocumentType =
                        selectedItem.DocumentType,

                    ProjectRootPath =
                        projectRootPath,

                    TocPath =
                        tocPathTextBox.Text,

                    TargetPath =
                        targetPathTextBox.Text,

                    DocumentTitle =
                        titleTextBox.Text,

                    DeviceName =
                        deviceTextBox.Text,

                    DocumentReference =
                        referenceTextBox.Text,

                    DocumentIndex =
                        indexTextBox.Text,

                    Language =
                        languageTextBox.Text,

                    MrefReference =
                        mrefTextBox.Text,

                    CopyResources =
                        copyResourcesCheckBox.Checked,

                    CleanToc =
                        cleanTocCheckBox.Checked,

                    UpdateVariables =
                        updateVariablesCheckBox.Checked,

                    ConfigureTarget =
                        configureTargetCheckBox.Checked

                   
                };

            runButton.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                AitImportFinalizerService service =
                    new AitImportFinalizerService();

                AitImportFinalizerReport report =
                    service.Run(options);

                MessageBoxIcon icon =
                    report.Errors.Count > 0
                        ? MessageBoxIcon.Warning
                        : MessageBoxIcon.Information;

                MessageBox.Show(
                    this,
                    BuildReportMessage(
                        report,
                        options),
                    "AIT Import Finalizer",
                    MessageBoxButtons.OK,
                    icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Une erreur inattendue est survenue "
                    + "pendant la finalisation :\n\n"
                    + ex.Message,
                    "AIT Import Finalizer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                runButton.Enabled = true;
            }
        }

        /// <summary>
        /// Affiche un message d'erreur de validation.
        /// </summary>
        private void ShowValidationMessage(
            string message)
        {
            MessageBox.Show(
                this,
                message,
                "AIT Import Finalizer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Retrouve le dossier racine du projet Flare
        /// à partir du chemin de la TOC.
        /// </summary>
        private string GetProjectRootPathFromToc(
            string tocPath)
        {
            if (string.IsNullOrWhiteSpace(
                tocPath))
            {
                return string.Empty;
            }

            if (!File.Exists(
                tocPath))
            {
                return string.Empty;
            }

            string tocDirectoryPath =
                Path.GetDirectoryName(
                    tocPath);

            if (string.IsNullOrWhiteSpace(
                tocDirectoryPath))
            {
                return string.Empty;
            }

            DirectoryInfo directory =
                new DirectoryInfo(
                    tocDirectoryPath);

            while (directory != null)
            {
                if (directory.Name.Equals(
                    "Project",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (directory.Parent != null)
                    {
                        return directory.Parent.FullName;
                    }

                    return string.Empty;
                }

                directory = directory.Parent;
            }

            return string.Empty;
        }

        /// <summary>
        /// Construit un rapport compréhensible par l'utilisateur.
        /// Une action décochée est indiquée comme non sélectionnée
        /// et non comme ayant échoué.
        /// </summary>
        private string BuildReportMessage(
            AitImportFinalizerReport report,
            AitImportFinalizerOptions options)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "FINALISATION DE L'IMPORT");

            builder.AppendLine();

            builder.AppendLine(
                "Profil : "
                + report.ProfileName);

            builder.AppendLine();

            AppendActionResult(
                builder,
                "Ressources ATLAS",
                options.CopyResources,
                report.ResourcesCopied,
                "Les ressources ont été installées "
                + "ou mises à jour dans le projet.");

            string tocDetails =
                "TOC : "
                + Path.GetFileName(
                    options.TocPath);

            if (report.TocEntriesRemoved != null)
            {
                tocDetails +=
                    "\n    "
                    + report.TocEntriesRemoved.Count
                    + " entrée(s) supprimée(s).";
            }

            AppendActionResult(
                builder,
                "Nettoyage de la TOC",
                options.CleanToc,
                report.TocCleaned,
                tocDetails);

            AppendActionResult(
                builder,
                "Variables générales",
                options.UpdateVariables,
                report.VariablesUpdated,
                "Project/VariableSets/General.flvar "
                + "a été mis à jour.");

            AppendActionResult(
                builder,
                "Configuration de la target",
                options.ConfigureTarget,
                report.TargetConfigured,
                "Target : "
                + Path.GetFileName(
                    options.TargetPath)
                + "\n    TOC, feuille de style et "
                + "layout primaire configurés.");

            if (report.Warnings != null
                && report.Warnings.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine(
                    "Informations / avertissements :");

                foreach (string warning
                    in report.Warnings)
                {
                    builder.AppendLine(
                        "- "
                        + warning);
                }
            }

            if (report.Errors != null
                && report.Errors.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine(
                    "Erreurs :");

                foreach (string error
                    in report.Errors)
                {
                    builder.AppendLine(
                        "- "
                        + error);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Ajoute le résultat d'une action au rapport.
        /// </summary>
        private void AppendActionResult(
            StringBuilder builder,
            string actionName,
            bool requested,
            bool succeeded,
            string successDetails)
        {
            if (!requested)
            {
                builder.AppendLine(
                    "- "
                    + actionName
                    + " : action non sélectionnée");

                return;
            }

            if (succeeded)
            {
                builder.AppendLine(
                    "[OK] "
                    + actionName);

                if (!string.IsNullOrWhiteSpace(
                    successDetails))
                {
                    builder.AppendLine(
                        "    "
                        + successDetails);
                }

                return;
            }

            builder.AppendLine(
                "[ÉCHEC] "
                + actionName);
        }

        /// <summary>
        /// Élément affiché dans la liste des types de documents.
        /// </summary>
        private class ComboItem
        {
            public string Text
            {
                get;
                private set;
            }

            public AitDocumentType DocumentType
            {
                get;
                private set;
            }

            public ComboItem(
                string text,
                AitDocumentType documentType)
            {
                Text = text;
                DocumentType = documentType;
            }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}