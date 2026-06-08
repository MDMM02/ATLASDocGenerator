using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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

        private CheckBox cbActionResults;
        private CheckBox cbBulletLists;
        private CheckBox cbCallouts;
        private CheckBox cbFigures;
        private CheckBox cbStyleCleanup;
        private CheckBox cbIhm;

        private Button btnRun;
        private Button btnCancel;

        public AitCleanupForm()
        {
            InitializeComponent();
            UpdateScopeState();
        }

        private void InitializeComponent()
        {
            Text = "AIT Cleanup";
            Width = 620;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label title = new Label();
            title.Text = "Author-it Cleanup";
            title.Font = new Font(Font.FontFamily, 14, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(20, 20);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Sélectionner le périmètre et les traitements à appliquer après import Author-it.";
            subtitle.AutoSize = true;
            subtitle.Location = new Point(22, 55);
            Controls.Add(subtitle);

            GroupBox docTypeGroup = new GroupBox();
            docTypeGroup.Text = "Type de documentation";
            docTypeGroup.Location = new Point(20, 90);
            docTypeGroup.Size = new Size(560, 80);
            Controls.Add(docTypeGroup);

            rbUserDoc = new RadioButton();
            rbUserDoc.Text = "Doc user / STR — appliquer à tout le projet";
            rbUserDoc.Location = new Point(15, 25);
            rbUserDoc.AutoSize = true;
            rbUserDoc.Checked = true;
            rbUserDoc.CheckedChanged += OnDocTypeChanged;
            docTypeGroup.Controls.Add(rbUserDoc);

            rbTechDoc = new RadioButton();
            rbTechDoc.Text = "Doc tech — appliquer au sous-dossier importé";
            rbTechDoc.Location = new Point(15, 50);
            rbTechDoc.AutoSize = true;
            rbTechDoc.CheckedChanged += OnDocTypeChanged;
            docTypeGroup.Controls.Add(rbTechDoc);

            GroupBox scopeGroup = new GroupBox();
            scopeGroup.Text = "Périmètre du cleanup";
            scopeGroup.Location = new Point(20, 185);
            scopeGroup.Size = new Size(560, 110);
            Controls.Add(scopeGroup);

            rbWholeProject = new RadioButton();
            rbWholeProject.Text = "Tout le projet";
            rbWholeProject.Location = new Point(15, 25);
            rbWholeProject.AutoSize = true;
            rbWholeProject.Checked = true;
            rbWholeProject.CheckedChanged += OnScopeChanged;
            scopeGroup.Controls.Add(rbWholeProject);

            rbSelectedFolder = new RadioButton();
            rbSelectedFolder.Text = "Sous-dossier sélectionné";
            rbSelectedFolder.Location = new Point(15, 50);
            rbSelectedFolder.AutoSize = true;
            rbSelectedFolder.CheckedChanged += OnScopeChanged;
            scopeGroup.Controls.Add(rbSelectedFolder);

            txtSelectedFolder = new TextBox();
            txtSelectedFolder.Location = new Point(35, 75);
            txtSelectedFolder.Width = 390;
            scopeGroup.Controls.Add(txtSelectedFolder);

            btnBrowseFolder = new Button();
            btnBrowseFolder.Text = "Parcourir...";
            btnBrowseFolder.Location = new Point(435, 73);
            btnBrowseFolder.Width = 100;
            btnBrowseFolder.Click += OnBrowseFolderClicked;
            scopeGroup.Controls.Add(btnBrowseFolder);

            GroupBox cleanupGroup = new GroupBox();
            cleanupGroup.Text = "Traitements à appliquer";
            cleanupGroup.Location = new Point(20, 310);
            cleanupGroup.Size = new Size(560, 115);
            Controls.Add(cleanupGroup);

            cbActionResults = new CheckBox();
            cbActionResults.Text = "Listes actions / résultats";
            cbActionResults.Location = new Point(15, 25);
            cbActionResults.AutoSize = true;
            cbActionResults.Checked = true;
            cleanupGroup.Controls.Add(cbActionResults);

            cbBulletLists = new CheckBox();
            cbBulletLists.Text = "Listes à tirets";
            cbBulletLists.Location = new Point(15, 50);
            cbBulletLists.AutoSize = true;
            cbBulletLists.Checked = true;
            cleanupGroup.Controls.Add(cbBulletLists);

            cbCallouts = new CheckBox();
            cbCallouts.Text = "Encadrés Information / Précaution / Attention";
            cbCallouts.Location = new Point(15, 75);
            cbCallouts.AutoSize = true;
            cbCallouts.Checked = true;
            cleanupGroup.Controls.Add(cbCallouts);

            cbFigures = new CheckBox();
            cbFigures.Text = "Images avec légendes";
            cbFigures.Location = new Point(295, 25);
            cbFigures.AutoSize = true;
            cbFigures.Checked = true;
            cleanupGroup.Controls.Add(cbFigures);

            cbStyleCleanup = new CheckBox();
            cbStyleCleanup.Text = "Cleanup styles simples";
            cbStyleCleanup.Location = new Point(295, 50);
            cbStyleCleanup.AutoSize = true;
            cbStyleCleanup.Checked = true;
            cleanupGroup.Controls.Add(cbStyleCleanup);

            cbIhm = new CheckBox();
            cbIhm.Text = "IHM / variables — phase 2";
            cbIhm.Location = new Point(295, 75);
            cbIhm.AutoSize = true;
            cbIhm.Checked = false;
            cleanupGroup.Controls.Add(cbIhm);

            btnRun = new Button();
            btnRun.Text = "Lancer";
            btnRun.Location = new Point(385, 440);
            btnRun.Width = 90;
            btnRun.Click += OnRunClicked;
            Controls.Add(btnRun);

            btnCancel = new Button();
            btnCancel.Text = "Annuler";
            btnCancel.Location = new Point(490, 440);
            btnCancel.Width = 90;
            btnCancel.Click += OnCancelClicked;
            Controls.Add(btnCancel);
        }

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
            bool folderMode = rbSelectedFolder.Checked;
            txtSelectedFolder.Enabled = folderMode;
            btnBrowseFolder.Enabled = folderMode;
        }

        private void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Sélectionner le sous-dossier importé depuis Author-it";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtSelectedFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnRunClicked(object sender, EventArgs e)
        {
            if (rbSelectedFolder.Checked && string.IsNullOrWhiteSpace(txtSelectedFolder.Text))
            {
                MessageBox.Show(
                    "Veuillez sélectionner un sous-dossier avant de lancer le cleanup.",
                    "AIT Cleanup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("AIT Cleanup prêt à être lancé.");
            summary.AppendLine();
            summary.AppendLine("Type de documentation : " + (rbUserDoc.Checked ? "Doc user / STR" : "Doc tech"));
            summary.AppendLine("Scope : " + (rbWholeProject.Checked ? "Tout le projet" : "Sous-dossier sélectionné"));

            if (rbSelectedFolder.Checked)
            {
                summary.AppendLine("Dossier : " + txtSelectedFolder.Text);
            }

            summary.AppendLine();
            summary.AppendLine("Traitements sélectionnés :");
            summary.AppendLine("- Listes actions/résultats : " + YesNo(cbActionResults.Checked));
            summary.AppendLine("- Listes à tirets : " + YesNo(cbBulletLists.Checked));
            summary.AppendLine("- Encadrés : " + YesNo(cbCallouts.Checked));
            summary.AppendLine("- Images avec légendes : " + YesNo(cbFigures.Checked));
            summary.AppendLine("- Cleanup styles simples : " + YesNo(cbStyleCleanup.Checked));
            summary.AppendLine("- IHM / variables : " + YesNo(cbIhm.Checked));
            summary.AppendLine();
            summary.AppendLine("Aucune transformation XML n'est encore exécutée à cette étape.");

            MessageBox.Show(
                summary.ToString(),
                "AIT Cleanup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private string YesNo(bool value)
        {
            return value ? "Oui" : "Non";
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            Close();
        }
    }
}