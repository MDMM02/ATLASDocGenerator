using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services;
using ATLASDocGenerator.Services.AitCleanup.IhmVariables;
using ATLASDocGenerator.Services.AitImportFinalizer;

namespace ATLASDocGenerator.Forms
{
    public class AitWorkflowForm : Form
    {
        private readonly string projectRoot;
        private readonly FrenchIhmTemplateDetector templateDetector;
        private ComboBox documentTypeComboBox;
        private ComboBox tocComboBox;
        private ComboBox targetComboBox;
        private TextBox cleanupFolderTextBox;
        private TextBox sourceXmlTextBox;
        private CheckedListBox ihmTemplatesList;
        private CheckBox installResourcesCheckBox;
        private CheckBox cleanContentCheckBox;
        private CheckBox processIhmCheckBox;
        private CheckBox cleanTocCheckBox;
        private CheckBox verifyTargetCheckBox;
        private CheckBox repairTargetCheckBox;
        private CheckBox generateReportCheckBox;
        private CheckBox actionResultsCheckBox;
        private CheckBox bulletsCheckBox;
        private CheckBox calloutsCheckBox;
        private CheckBox figuresCheckBox;
        private CheckBox stylesCheckBox;

        public AitWorkflowForm(string projectRootPath)
        {
            projectRoot = projectRootPath;
            templateDetector = new FrenchIhmTemplateDetector();
            InitializeComponent();
            LoadProjectFiles();
            UpdateControlStates();
        }

        private void InitializeComponent()
        {
            Text = "Finaliser import AIT - ATLAS";
            ClientSize = new Size(760, 820);
            MinimumSize = new Size(760, 680);
            StartPosition = FormStartPosition.CenterParent;
            AutoScroll = true;
            Font = new Font("Segoe UI", 9);

            Controls.Add(new Label
            {
                Text = "Finaliser un import Author-it",
                Left = 20,
                Top = 18,
                Width = 700,
                Height = 28,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            });
            Controls.Add(new Label
            {
                Text = "Projet actif : " + projectRoot,
                Left = 20,
                Top = 50,
                Width = 710,
                Height = 25
            });

            GroupBox files = new GroupBox
            {
                Text = "1. Document et fichiers",
                Left = 20,
                Top = 82,
                Width = 710,
                Height = 205
            };
            Controls.Add(files);

            documentTypeComboBox = AddCombo(files, "Type de document :", 28);
            foreach (AitDocumentProfile profile in new AitDocumentProfileFactory().GetProfiles())
                documentTypeComboBox.Items.Add(new DocumentTypeItem(profile));
            documentTypeComboBox.SelectedIndex = 0;

            tocComboBox = AddCombo(files, "TOC :", 66);
            targetComboBox = AddCombo(files, "Target :", 104);
            targetComboBox.SelectedIndexChanged += TargetSelectionChanged;

            AddLabel(files, "Dossier à nettoyer :", 142);
            cleanupFolderTextBox = new TextBox { Left = 185, Top = 138, Width = 405 };
            Button browseFolder = new Button { Text = "Parcourir", Left = 600, Top = 136, Width = 90 };
            browseFolder.Click += BrowseFolderClicked;
            files.Controls.Add(cleanupFolderTextBox);
            files.Controls.Add(browseFolder);

            GroupBox actions = new GroupBox
            {
                Text = "2. Actions",
                Left = 20,
                Top = 300,
                Width = 710,
                Height = 225
            };
            Controls.Add(actions);

            installResourcesCheckBox = AddAction(actions, "Installer ou mettre à jour les ressources ATLAS", 25, true);
            cleanContentCheckBox = AddAction(actions, "Nettoyer les topics et snippets", 53, true);
            processIhmCheckBox = AddAction(actions, "Traiter les variables IHM", 81, false);
            cleanTocCheckBox = AddAction(actions, "Nettoyer la TOC sans modifier sa hiérarchie", 109, true);
            verifyTargetCheckBox = AddAction(actions, "Vérifier la target sans la modifier", 137, true);
            repairTargetCheckBox = AddAction(actions, "Réparer TOC, CSS, layout et niveaux de titres si nécessaire", 165, false);
            generateReportCheckBox = AddAction(actions, "Générer le rapport final unifié", 193, true);

            cleanContentCheckBox.CheckedChanged += AnyOptionChanged;
            processIhmCheckBox.CheckedChanged += AnyOptionChanged;
            verifyTargetCheckBox.CheckedChanged += AnyOptionChanged;
            repairTargetCheckBox.CheckedChanged += RepairTargetChanged;

            GroupBox advanced = new GroupBox
            {
                Text = "3. Options Cleanup",
                Left = 20,
                Top = 538,
                Width = 710,
                Height = 82
            };
            Controls.Add(advanced);
            actionResultsCheckBox = AddAdvanced(advanced, "Actions / résultats", 15, 25);
            bulletsCheckBox = AddAdvanced(advanced, "Listes à tirets", 235, 25);
            calloutsCheckBox = AddAdvanced(advanced, "Callouts", 455, 25);
            figuresCheckBox = AddAdvanced(advanced, "Figures", 15, 52);
            stylesCheckBox = AddAdvanced(advanced, "Styles simples", 235, 52);

            GroupBox ihm = new GroupBox
            {
                Text = "4. Source IHM (uniquement si l’option IHM est cochée)",
                Left = 20,
                Top = 633,
                Width = 710,
                Height = 115
            };
            Controls.Add(ihm);
            sourceXmlTextBox = new TextBox { Left = 15, Top = 25, Width = 575 };
            Button browseXml = new Button { Text = "XML...", Left = 600, Top = 23, Width = 90 };
            browseXml.Click += BrowseXmlClicked;
            ihmTemplatesList = new CheckedListBox
            {
                Left = 15,
                Top = 55,
                Width = 675,
                Height = 50,
                CheckOnClick = true,
                HorizontalScrollbar = true
            };
            ihm.Controls.Add(sourceXmlTextBox);
            ihm.Controls.Add(browseXml);
            ihm.Controls.Add(ihmTemplatesList);

            Button run = new Button { Text = "Finaliser", Left = 535, Top = 770, Width = 95 };
            run.Click += RunClicked;
            Button cancel = new Button
            {
                Text = "Annuler",
                Left = 640,
                Top = 770,
                Width = 90,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(run);
            Controls.Add(cancel);
            AcceptButton = run;
            CancelButton = cancel;
        }

        private void LoadProjectFiles()
        {
            cleanupFolderTextBox.Text = Path.Combine(projectRoot, "Content");
            LoadFilesIntoCombo(
                tocComboBox,
                Path.Combine(projectRoot, "Project", "TOCs"),
                "*.fltoc");
            LoadFilesIntoCombo(
                targetComboBox,
                Path.Combine(projectRoot, "Project", "Targets"),
                "*.fltar");
        }

        private void LoadFilesIntoCombo(ComboBox combo, string root, string pattern)
        {
            if (!Directory.Exists(root))
                return;
            foreach (string path in Directory.GetFiles(root, pattern, SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase))
            {
                combo.Items.Add(new PathItem(root, path));
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void TargetSelectionChanged(object sender, EventArgs e)
        {
            PathItem target = targetComboBox.SelectedItem as PathItem;
            if (target == null)
                return;
            try
            {
                XDocument document = XDocument.Load(target.FullPath);
                XAttribute masterToc = document.Root == null
                    ? null
                    : document.Root.Attributes().FirstOrDefault(attribute =>
                        attribute.Name.LocalName.Equals("MasterToc", StringComparison.OrdinalIgnoreCase));
                if (masterToc == null)
                    return;
                string tocPath = Path.GetFullPath(Path.Combine(
                    projectRoot,
                    Uri.UnescapeDataString(masterToc.Value.TrimStart('/', '\\'))
                        .Replace('/', Path.DirectorySeparatorChar)));
                for (int index = 0; index < tocComboBox.Items.Count; index++)
                {
                    PathItem item = tocComboBox.Items[index] as PathItem;
                    if (item != null && item.FullPath.Equals(tocPath, StringComparison.OrdinalIgnoreCase))
                    {
                        tocComboBox.SelectedIndex = index;
                        break;
                    }
                }
            }
            catch
            {
                // La validation détaillée sera affichée au lancement.
            }
        }

        private void BrowseFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Sélectionner le dossier importé à nettoyer.";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    cleanupFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseXmlClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                sourceXmlTextBox.Text = dialog.FileName;
                LoadIhmTemplates(dialog.FileName);
            }
        }

        private void LoadIhmTemplates(string path)
        {
            ihmTemplatesList.Items.Clear();
            foreach (FrenchIhmTemplateInfo template in templateDetector.Detect(path))
                ihmTemplatesList.Items.Add(template, false);
        }

        private void AnyOptionChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
        }

        private void RepairTargetChanged(object sender, EventArgs e)
        {
            if (repairTargetCheckBox.Checked)
                verifyTargetCheckBox.Checked = true;
            UpdateControlStates();
        }

        private void UpdateControlStates()
        {
            bool cleanupEnabled = cleanContentCheckBox != null && cleanContentCheckBox.Checked;
            if (actionResultsCheckBox != null)
            {
                actionResultsCheckBox.Enabled = cleanupEnabled;
                bulletsCheckBox.Enabled = cleanupEnabled;
                calloutsCheckBox.Enabled = cleanupEnabled;
                figuresCheckBox.Enabled = cleanupEnabled;
                stylesCheckBox.Enabled = cleanupEnabled;
            }
            bool ihmEnabled = processIhmCheckBox != null && processIhmCheckBox.Checked;
            if (sourceXmlTextBox != null)
            {
                sourceXmlTextBox.Enabled = ihmEnabled;
                ihmTemplatesList.Enabled = ihmEnabled;
            }
            if (repairTargetCheckBox != null)
            {
                if (!verifyTargetCheckBox.Checked && repairTargetCheckBox.Checked)
                    repairTargetCheckBox.Checked = false;
                repairTargetCheckBox.Enabled = verifyTargetCheckBox.Checked;
            }
        }

        private void RunClicked(object sender, EventArgs e)
        {
            try
            {
                PathItem toc = tocComboBox.SelectedItem as PathItem;
                PathItem target = targetComboBox.SelectedItem as PathItem;
                DocumentTypeItem documentType = documentTypeComboBox.SelectedItem as DocumentTypeItem;
                if (documentType == null)
                    throw new InvalidOperationException("Sélectionnez un type de document.");
                if ((cleanTocCheckBox.Checked || verifyTargetCheckBox.Checked) && toc == null)
                    throw new InvalidOperationException("Sélectionnez une TOC.");
                if (verifyTargetCheckBox.Checked && target == null)
                    throw new InvalidOperationException("Sélectionnez une target.");
                if ((cleanContentCheckBox.Checked || processIhmCheckBox.Checked)
                    && !Directory.Exists(cleanupFolderTextBox.Text))
                    throw new DirectoryNotFoundException("Le dossier à nettoyer est introuvable.");
                if (processIhmCheckBox.Checked)
                {
                    if (!File.Exists(sourceXmlTextBox.Text))
                        throw new FileNotFoundException("Le XML Author-it est introuvable.", sourceXmlTextBox.Text);
                    if (ihmTemplatesList.CheckedItems.Count == 0)
                        throw new InvalidOperationException("Sélectionnez au moins un template IHM.");
                }

                AitCleanupOptions cleanup = new AitCleanupOptions
                {
                    DocumentationType = documentType.Profile.DisplayName,
                    Scope = AitCleanupScope.SelectedFolder,
                    TargetPath = cleanupFolderTextBox.Text,
                    SourceXmlPath = sourceXmlTextBox.Text,
                    SelectedIhmTemplateIds = ihmTemplatesList.CheckedItems
                        .Cast<FrenchIhmTemplateInfo>()
                        .Select(template => template.Id)
                        .ToList(),
                    ProcessActionResults = actionResultsCheckBox.Checked,
                    ProcessBulletLists = bulletsCheckBox.Checked,
                    ProcessCallouts = calloutsCheckBox.Checked,
                    ProcessFigures = figuresCheckBox.Checked,
                    ProcessStyleCleanup = stylesCheckBox.Checked,
                    ProcessIhm = processIhmCheckBox.Checked
                };

                AitWorkflowReport report = new AitWorkflowService().Run(new AitWorkflowOptions
                {
                    DocumentType = documentType.Profile.DocumentType,
                    ProjectRootPath = projectRoot,
                    TocPath = toc == null ? string.Empty : toc.FullPath,
                    TargetPath = target == null ? string.Empty : target.FullPath,
                    InstallResources = installResourcesCheckBox.Checked,
                    CleanContent = cleanContentCheckBox.Checked,
                    ProcessIhm = processIhmCheckBox.Checked,
                    CleanToc = cleanTocCheckBox.Checked,
                    VerifyTarget = verifyTargetCheckBox.Checked,
                    RepairTarget = repairTargetCheckBox.Checked,
                    GenerateReport = generateReportCheckBox.Checked,
                    CleanupOptions = cleanup
                });

                ShowSummary(report);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Finaliser import AIT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowSummary(AitWorkflowReport report)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Finalisation AIT terminée.");
            summary.AppendLine("Profil : " + report.ProfileName);
            summary.AppendLine("General.flvar existant : préservé");
            if (report.ResourceCopyResult != null)
            {
                summary.AppendLine("Ressources ajoutées : " + report.ResourceCopyResult.FilesCopied);
                summary.AppendLine("Ressources mises à jour : " + report.ResourceCopyResult.FilesUpdated);
                summary.AppendLine("Ressources préservées : " + report.ResourceCopyResult.FilesPreserved);
            }
            if (report.CleanupReport != null)
            {
                summary.AppendLine("Fichiers scannés : " + report.CleanupReport.FilesScanned);
                summary.AppendLine(
                    "Transformations : "
                    + (report.CleanupReport.ActionResultListsTransformed
                       + report.CleanupReport.BulletListsTransformed
                       + report.CleanupReport.CalloutsTransformed
                       + report.CleanupReport.FiguresTransformed
                       + report.CleanupReport.StylesCleaned));
                summary.AppendLine("Références IHM remplacées : " + report.CleanupReport.IhmReferencesReplaced);
            }
            summary.AppendLine("Entrées TOC supprimées : " + report.TocEntriesRemoved.Count);
            if (report.TargetValidation != null)
                summary.AppendLine("Target conforme : " + (report.TargetValidation.IsValid ? "oui" : "non"));
            summary.AppendLine("Target réparée : " + (report.TargetRepaired ? "oui" : "non"));
            summary.AppendLine("Avertissements : " + report.Warnings.Count);
            summary.AppendLine("Erreurs : " + report.Errors.Count);
            if (!string.IsNullOrWhiteSpace(report.ReportFilePath))
            {
                summary.AppendLine();
                summary.AppendLine("Rapport : " + report.ReportFilePath);
            }
            MessageBox.Show(
                summary.ToString(),
                "Finaliser import AIT",
                MessageBoxButtons.OK,
                report.Errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private ComboBox AddCombo(Control parent, string label, int top)
        {
            AddLabel(parent, label, top + 4);
            ComboBox combo = new ComboBox
            {
                Left = 185,
                Top = top,
                Width = 505,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            parent.Controls.Add(combo);
            return combo;
        }

        private void AddLabel(Control parent, string text, int top)
        {
            parent.Controls.Add(new Label { Text = text, Left = 15, Top = top, Width = 165 });
        }

        private CheckBox AddAction(Control parent, string text, int top, bool value)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text,
                Left = 18,
                Top = top,
                Width = 665,
                Checked = value
            };
            parent.Controls.Add(checkBox);
            return checkBox;
        }

        private CheckBox AddAdvanced(Control parent, string text, int left, int top)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 205,
                Checked = true
            };
            parent.Controls.Add(checkBox);
            return checkBox;
        }

        private class PathItem
        {
            public PathItem(string root, string path)
            {
                FullPath = path;
                DisplayName = path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            public string FullPath { get; private set; }
            public string DisplayName { get; private set; }
            public override string ToString() { return DisplayName; }
        }

        private class DocumentTypeItem
        {
            public DocumentTypeItem(AitDocumentProfile profile) { Profile = profile; }
            public AitDocumentProfile Profile { get; private set; }
            public override string ToString() { return Profile.DisplayName; }
        }
    }
}
