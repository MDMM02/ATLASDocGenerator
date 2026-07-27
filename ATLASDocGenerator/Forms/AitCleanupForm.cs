using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services.AitCleanup;
using ATLASDocGenerator.Services.AitCleanup.IhmVariables;

namespace ATLASDocGenerator.Forms
{
    public class AitCleanupForm : Form
    {
        private RadioButton rbUserDoc;
        private RadioButton rbTechDoc;

        private RadioButton rbWholeProject;
        private RadioButton rbSelectedFolder;

        private TextBox txtSelectedFolder;
        private Button btnBrowseFolder;

        private TextBox txtSourceXmlPath;
        private Button btnBrowseSourceXml;

        private CheckedListBox clbIhmTemplates;
        private Label lblIhmTemplatesStatus;

        private CheckBox cbActionResults;
        private CheckBox cbBulletLists;
        private CheckBox cbCallouts;
        private CheckBox cbFigures;
        private CheckBox cbStyleCleanup;
        private CheckBox cbIhm;

        private Button btnRun;
        private Button btnCancel;

        private readonly FrenchIhmTemplateDetector _ihmTemplateDetector;

        public AitCleanupForm()
        {
            _ihmTemplateDetector = new FrenchIhmTemplateDetector();

            InitializeComponent();
            UpdateScopeState();
            UpdateIhmState();
        }

        private void InitializeComponent()
        {
            Text = "AIT Cleanup";

            ClientSize = new Size(620, 780);

            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            MaximizeBox = false;
            MinimizeBox = false;

            /*
             * TITRE
             */

            Label title = new Label
            {
                Text = "Author-it Cleanup",
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "Sélectionner le périmètre et les traitements à appliquer après import Author-it.",
                AutoSize = true,
                Location = new Point(22, 55)
            };

            Controls.Add(subtitle);

            /*
             * TYPE DE DOCUMENTATION
             */

            GroupBox docTypeGroup = new GroupBox
            {
                Text = "Type de documentation",
                Location = new Point(20, 90),
                Size = new Size(580, 80)
            };

            Controls.Add(docTypeGroup);

            rbUserDoc = new RadioButton
            {
                Text = "Doc user  — appliquer à tout le projet",
                Location = new Point(15, 25),
                AutoSize = true,
                Checked = true
            };

            rbUserDoc.CheckedChanged += OnDocTypeChanged;
            docTypeGroup.Controls.Add(rbUserDoc);

            rbTechDoc = new RadioButton
            {
                Text = "Doc tech — appliquer au sous-dossier importé",
                Location = new Point(15, 50),
                AutoSize = true
            };

            rbTechDoc.CheckedChanged += OnDocTypeChanged;
            docTypeGroup.Controls.Add(rbTechDoc);

            /*
             * PÉRIMÈTRE
             */

            GroupBox scopeGroup = new GroupBox
            {
                Text = "Périmètre du cleanup",
                Location = new Point(20, 185),
                Size = new Size(580, 110)
            };

            Controls.Add(scopeGroup);

            rbWholeProject = new RadioButton
            {
                Text = "Tout le projet",
                Location = new Point(15, 25),
                AutoSize = true,
                Checked = true
            };

            rbWholeProject.CheckedChanged += OnScopeChanged;
            scopeGroup.Controls.Add(rbWholeProject);

            rbSelectedFolder = new RadioButton
            {
                Text = "Sous-dossier sélectionné",
                Location = new Point(15, 50),
                AutoSize = true
            };

            rbSelectedFolder.CheckedChanged += OnScopeChanged;
            scopeGroup.Controls.Add(rbSelectedFolder);

            txtSelectedFolder = new TextBox
            {
                Location = new Point(35, 75),
                Width = 410
            };

            scopeGroup.Controls.Add(txtSelectedFolder);

            btnBrowseFolder = new Button
            {
                Text = "Parcourir...",
                Location = new Point(455, 73),
                Width = 100
            };

            btnBrowseFolder.Click += OnBrowseFolderClicked;
            scopeGroup.Controls.Add(btnBrowseFolder);

            /*
             * XML AUTHOR-IT
             */

            GroupBox xmlGroup = new GroupBox
            {
                Text = "Source XML Author-it",
                Location = new Point(20, 310),
                Size = new Size(580, 80)
            };

            Controls.Add(xmlGroup);

            Label xmlLabel = new Label
            {
                Text = "XML source utilisé pour détecter les templates et générer les variables IHM :",
                Location = new Point(15, 21),
                AutoSize = true
            };

            xmlGroup.Controls.Add(xmlLabel);

            txtSourceXmlPath = new TextBox
            {
                Location = new Point(15, 47),
                Width = 430
            };

            xmlGroup.Controls.Add(txtSourceXmlPath);

            btnBrowseSourceXml = new Button
            {
                Text = "Parcourir...",
                Location = new Point(455, 45),
                Width = 100
            };

            btnBrowseSourceXml.Click += OnBrowseSourceXmlClicked;
            xmlGroup.Controls.Add(btnBrowseSourceXml);

            /*
             * TEMPLATES IHM
             */

            GroupBox ihmTemplatesGroup = new GroupBox
            {
                Text = "Templates IHM français",
                Location = new Point(20, 405),
                Size = new Size(580, 175)
            };

            Controls.Add(ihmTemplatesGroup);

            Label templateLabel = new Label
            {
                Text = "Sélectionner les templates IHM à convertir en fichiers de variables :",
                Location = new Point(15, 22),
                AutoSize = true
            };

            ihmTemplatesGroup.Controls.Add(templateLabel);

            clbIhmTemplates = new CheckedListBox
            {
                Location = new Point(15, 47),
                Size = new Size(540, 94),
                CheckOnClick = true,
                HorizontalScrollbar = true
            };

            ihmTemplatesGroup.Controls.Add(clbIhmTemplates);

            lblIhmTemplatesStatus = new Label
            {
                Text = "Sélectionner d'abord un fichier XML Author-it.",
                Location = new Point(15, 146),
                AutoSize = true
            };

            ihmTemplatesGroup.Controls.Add(lblIhmTemplatesStatus);

            /*
             * TRAITEMENTS
             */

            GroupBox cleanupGroup = new GroupBox
            {
                Text = "Traitements à appliquer",
                Location = new Point(20, 595),
                Size = new Size(580, 115)
            };

            Controls.Add(cleanupGroup);

            cbActionResults = new CheckBox
            {
                Text = "Listes actions / résultats",
                Location = new Point(15, 25),
                AutoSize = true,
                Checked = true
            };

            cleanupGroup.Controls.Add(cbActionResults);

            cbBulletLists = new CheckBox
            {
                Text = "Listes à tirets",
                Location = new Point(15, 50),
                AutoSize = true,
                Checked = true
            };

            cleanupGroup.Controls.Add(cbBulletLists);

            cbCallouts = new CheckBox
            {
                Text = "Encadrés Information / Précaution / Attention",
                Location = new Point(15, 75),
                AutoSize = true,
                Checked = true
            };

            cleanupGroup.Controls.Add(cbCallouts);

            cbFigures = new CheckBox
            {
                Text = "Images avec légendes",
                Location = new Point(310, 25),
                AutoSize = true,
                Checked = true
            };

            cleanupGroup.Controls.Add(cbFigures);

            cbStyleCleanup = new CheckBox
            {
                Text = "Cleanup styles simples",
                Location = new Point(310, 50),
                AutoSize = true,
                Checked = true
            };

            cleanupGroup.Controls.Add(cbStyleCleanup);

            cbIhm = new CheckBox
            {
                Text = "Génération des variables IHM",
                Location = new Point(310, 75),
                AutoSize = true,
                Checked = false
            };

            cbIhm.CheckedChanged += OnIhmCheckedChanged;
            cleanupGroup.Controls.Add(cbIhm);

            /*
             * BOUTONS
             */

            btnRun = new Button
            {
                Text = "Lancer",
                Location = new Point(400, 730),
                Width = 90
            };

            btnRun.Click += OnRunClicked;
            Controls.Add(btnRun);

            btnCancel = new Button
            {
                Text = "Annuler",
                Location = new Point(505, 730),
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            btnCancel.Click += OnCancelClicked;
            Controls.Add(btnCancel);

            AcceptButton = btnRun;
            CancelButton = btnCancel;
        }

        /*
         * ÉVÉNEMENTS DOCUMENTATION / PÉRIMÈTRE
         */

        private void OnDocTypeChanged(object sender, EventArgs e)
        {
            if (rbUserDoc.Checked)
            {
                rbWholeProject.Checked = true;
            }
            else if (rbTechDoc.Checked)
            {
                rbSelectedFolder.Checked = true;
            }

            UpdateScopeState();
        }

        private void OnScopeChanged(object sender, EventArgs e)
        {
            UpdateScopeState();
        }

        private void UpdateScopeState()
        {
            txtSelectedFolder.Enabled = true;
            btnBrowseFolder.Enabled = true;
        }

        /*
         * SÉLECTION DU DOSSIER
         */

        private void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description =
                    "Sélectionner la racine du projet Flare, le dossier Content "
                    + "ou le sous-dossier importé depuis Author-it.";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtSelectedFolder.Text = dialog.SelectedPath;
                }
            }
        }

        /*
         * SÉLECTION DU XML ET DÉTECTION DES TEMPLATES
         */

        private void OnBrowseSourceXmlClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Sélectionner le XML Author-it source";

                dialog.Filter =
                    "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                txtSourceXmlPath.Text = dialog.FileName;

                LoadFrenchIhmTemplates(dialog.FileName);
            }
        }

        private void LoadFrenchIhmTemplates(string sourceXmlPath)
        {
            clbIhmTemplates.Items.Clear();

            lblIhmTemplatesStatus.Text =
                "Analyse du fichier XML en cours...";

            lblIhmTemplatesStatus.Refresh();

            try
            {
                List<FrenchIhmTemplateInfo> templates =
                    _ihmTemplateDetector.Detect(sourceXmlPath);

                foreach (FrenchIhmTemplateInfo template in templates)
                {
                    clbIhmTemplates.Items.Add(template, false);
                }

                if (templates.Count == 0)
                {
                    lblIhmTemplatesStatus.Text =
                        "Aucun template Topic français utilisé n'a été détecté.";

                    return;
                }

                lblIhmTemplatesStatus.Text =
                    templates.Count
                    + " template(s) Topic français utilisé(s) détecté(s).";
            }
            catch (Exception ex)
            {
                clbIhmTemplates.Items.Clear();

                lblIhmTemplatesStatus.Text =
                    "Erreur pendant l'analyse du fichier XML.";

                MessageBox.Show(
                    "Impossible de détecter les templates Author-it."
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "AIT Cleanup - Templates IHM",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ACTIVATION IHM
         */

        private void OnIhmCheckedChanged(object sender, EventArgs e)
        {
            UpdateIhmState();
        }

        private void UpdateIhmState()
        {
            bool enabled = cbIhm != null && cbIhm.Checked;

            txtSourceXmlPath.Enabled = enabled;
            btnBrowseSourceXml.Enabled = enabled;
            clbIhmTemplates.Enabled = enabled;
        }

        /*
         * LANCEMENT
         */

        private void OnRunClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSelectedFolder.Text))
            {
                MessageBox.Show(
                    "Veuillez sélectionner un dossier avant de lancer le cleanup.",
                    "AIT Cleanup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Directory.Exists(txtSelectedFolder.Text))
            {
                MessageBox.Show(
                    "Le dossier sélectionné n'existe pas.",
                    "AIT Cleanup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            /*
             * Validation IHM avant le lancement du service.
             */

            if (cbIhm.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtSourceXmlPath.Text))
                {
                    MessageBox.Show(
                        "Veuillez sélectionner le fichier XML Author-it source.",
                        "AIT Cleanup - IHM",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (!File.Exists(txtSourceXmlPath.Text))
                {
                    MessageBox.Show(
                        "Le fichier XML Author-it sélectionné n'existe pas.",
                        "AIT Cleanup - IHM",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (clbIhmTemplates.CheckedItems.Count == 0)
                {
                    MessageBox.Show(
                        "Veuillez sélectionner au moins un template IHM français.",
                        "AIT Cleanup - IHM",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }
            }

            List<string> selectedTemplateIds =
                clbIhmTemplates
                    .CheckedItems
                    .Cast<FrenchIhmTemplateInfo>()
                    .Select(template => template.Id)
                    .ToList();

            AitCleanupOptions options = new AitCleanupOptions
            {
                DocumentationType =
                    rbUserDoc.Checked
                        ? "Doc user"
                        : "Doc tech",

                Scope =
                    rbWholeProject.Checked
                        ? AitCleanupScope.WholeProject
                        : AitCleanupScope.SelectedFolder,

                TargetPath = txtSelectedFolder.Text,

                SourceXmlPath = txtSourceXmlPath.Text,

                SelectedIhmTemplateIds = selectedTemplateIds,

                ProcessActionResults = cbActionResults.Checked,
                ProcessBulletLists = cbBulletLists.Checked,
                ProcessCallouts = cbCallouts.Checked,
                ProcessFigures = cbFigures.Checked,
                ProcessStyleCleanup = cbStyleCleanup.Checked,
                ProcessIhm = cbIhm.Checked
            };

            AitCleanupService service = new AitCleanupService();
            CleanupReport report = service.Run(options);

            ShowCleanupSummary(report, selectedTemplateIds.Count);
        }

        /*
         * RÉSUMÉ
         */

        private void ShowCleanupSummary(
            CleanupReport report,
            int selectedIhmTemplateCount)
        {
            StringBuilder summary = new StringBuilder();

            summary.AppendLine("AIT Cleanup terminé.");
            summary.AppendLine();

            summary.AppendLine(
                "Fichiers scannés : "
                + report.FilesScanned);

            summary.AppendLine(
                "Dossier analysé : "
                + report.ScanRoot);

            summary.AppendLine();

            summary.AppendLine(
                "Actions numérotées détectées : "
                + report.ActionNumParagraphsDetected);

            summary.AppendLine(
                "Actions bullet détectées : "
                + report.ActionBulletParagraphsDetected);

            summary.AppendLine(
                "Résultats détectés : "
                + report.ResultParagraphsDetected);

            summary.AppendLine();

            summary.AppendLine(
                "Transformations actions/résultats appliquées : "
                + report.ActionResultListsTransformed);

            summary.AppendLine(
                "Listes à tirets transformées : "
                + report.BulletListsTransformed);

            summary.AppendLine(
                "Paragraphes tirets détectés : "
                + report.BulletParagraphsDetected);

            summary.AppendLine(
                "Blocs a_NOpagebreak créés : "
                + report.NoPageBreakBlocksCreated);

            if (cbIhm.Checked)
            {
                summary.AppendLine();

                summary.AppendLine(
                    "Templates IHM sélectionnés : "
                    + selectedIhmTemplateCount);

                summary.AppendLine(
                    "Jeux de variables générés : "
                    + report.IhmVariableSetsGenerated);

                summary.AppendLine(
                    "Variables générées : "
                    + report.IhmVariablesGenerated);

                summary.AppendLine(
                    "Fichiers analysés pour les références IHM : "
                    + report.IhmReferenceFilesScanned);

                summary.AppendLine(
                    "Fichiers modifiés : "
                    + report.IhmReferenceFilesModified);

                summary.AppendLine(
                    "Références de snippets remplacées : "
                    + report.IhmReferencesReplaced);

                summary.AppendLine(
                    "IDs Topic non associés : "
                    + report.IhmUnmatchedTopicIds);
            }

            if (report.Errors != null && report.Errors.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine(
                    "Erreurs : "
                    + report.Errors.Count);

                summary.AppendLine(
                    "Consulter le log pour plus de détails.");
            }

            summary.AppendLine();
            summary.AppendLine(
                "Important : vérifier les fichiers dans MadCap "
                + "et consulter le log avant de relancer le cleanup.");

            summary.AppendLine();

            summary.AppendLine("Log généré :");
            summary.AppendLine(report.LogFilePath);

            MessageBox.Show(
                summary.ToString(),
                "AIT Cleanup",
                MessageBoxButtons.OK,
                report.Errors != null && report.Errors.Count > 0
                    ? MessageBoxIcon.Warning
                    : MessageBoxIcon.Information);
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            Close();
        }
    }
}