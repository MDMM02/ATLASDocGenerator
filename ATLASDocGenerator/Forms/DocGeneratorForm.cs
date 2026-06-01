using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services;

namespace ATLASDocGenerator.Forms
{
    public class DocGeneratorForm : Form
    {
        private ComboBox cmbDocumentType;
        private TextBox txtProjectPath;
        private Button btnBrowse;
        private TextBox txtShortTitle;
        private TextBox txtDocumentReference;
        private ComboBox cmbDevice;
        private TextBox txtDeviceFree;
        private ComboBox cmbRange;
        private TextBox txtFullTitle;
        private Button btnGenerate;
        private Button btnCancel;

        public DocGeneratorForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Doc Generator - ATLAS";
            Width = 620;
            Height = 430;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int labelLeft = 20;
            int fieldLeft = 190;
            int top = 25;
            int rowHeight = 35;
            int fieldWidth = 340;

            AddLabel("Type de document", labelLeft, top);
            cmbDocumentType = new ComboBox();
            cmbDocumentType.Left = fieldLeft;
            cmbDocumentType.Top = top - 3;
            cmbDocumentType.Width = fieldWidth;
            cmbDocumentType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDocumentType.Items.Add("PS");
            cmbDocumentType.Items.Add("Notice");
            cmbDocumentType.SelectedIndex = 0;
            Controls.Add(cmbDocumentType);

            top += rowHeight;

            AddLabel("Dossier du projet", labelLeft, top);
            txtProjectPath = new TextBox();
            txtProjectPath.Left = fieldLeft;
            txtProjectPath.Top = top - 3;
            txtProjectPath.Width = 250;
            Controls.Add(txtProjectPath);

            btnBrowse = new Button();
            btnBrowse.Text = "Parcourir";
            btnBrowse.Left = fieldLeft + 260;
            btnBrowse.Top = top - 5;
            btnBrowse.Width = 80;
            btnBrowse.Click += BtnBrowse_Click;
            Controls.Add(btnBrowse);

            top += rowHeight;

            AddLabel("Titre doc abrégé", labelLeft, top);
            txtShortTitle = new TextBox();
            txtShortTitle.Left = fieldLeft;
            txtShortTitle.Top = top - 3;
            txtShortTitle.Width = fieldWidth;
            txtShortTitle.MaxLength = 40;
            Controls.Add(txtShortTitle);

            top += rowHeight;

            AddLabel("Référence sans indice", labelLeft, top);
            txtDocumentReference = new TextBox();
            txtDocumentReference.Left = fieldLeft;
            txtDocumentReference.Top = top - 3;
            txtDocumentReference.Width = fieldWidth;
            Controls.Add(txtDocumentReference);

            top += rowHeight;

            AddLabel("Dispositif", labelLeft, top);
            cmbDevice = new ComboBox();
            cmbDevice.Left = fieldLeft;
            cmbDevice.Top = top - 3;
            cmbDevice.Width = fieldWidth;
            cmbDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDevice.Items.Add("STA Compact");
            cmbDevice.Items.Add("Multi");
            cmbDevice.Items.Add("Autre");
            cmbDevice.SelectedIndex = 0;
            cmbDevice.SelectedIndexChanged += CmbDevice_SelectedIndexChanged;
            Controls.Add(cmbDevice);

            top += rowHeight;

            AddLabel("Nom dispositif libre", labelLeft, top);
            txtDeviceFree = new TextBox();
            txtDeviceFree.Left = fieldLeft;
            txtDeviceFree.Top = top - 3;
            txtDeviceFree.Width = fieldWidth;
            txtDeviceFree.Enabled = false;
            Controls.Add(txtDeviceFree);

            top += rowHeight;

            AddLabel("Gamme", labelLeft, top);
            cmbRange = new ComboBox();
            cmbRange.Left = fieldLeft;
            cmbRange.Top = top - 3;
            cmbRange.Width = fieldWidth;
            cmbRange.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRange.Items.Add("sthemX");
            cmbRange.Items.Add("Max");
            cmbRange.Items.Add("STA");
            cmbRange.SelectedIndex = 0;
            Controls.Add(cmbRange);

            top += rowHeight;

            AddLabel("Titre complet", labelLeft, top);
            txtFullTitle = new TextBox();
            txtFullTitle.Left = fieldLeft;
            txtFullTitle.Top = top - 3;
            txtFullTitle.Width = fieldWidth;
            txtFullTitle.MaxLength = 120;
            Controls.Add(txtFullTitle);

            top += 55;

            btnGenerate = new Button();
            btnGenerate.Text = "Générer";
            btnGenerate.Left = 350;
            btnGenerate.Top = top;
            btnGenerate.Width = 90;
            btnGenerate.Click += BtnGenerate_Click;
            Controls.Add(btnGenerate);

            btnCancel = new Button();
            btnCancel.Text = "Annuler";
            btnCancel.Left = 450;
            btnCancel.Top = top;
            btnCancel.Width = 90;
            btnCancel.Click += BtnCancel_Click;
            Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = 160;
            Controls.Add(label);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Sélectionner le dossier racine du projet MadCap";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    txtProjectPath.Text = dialog.SelectedPath;
            }
        }

        private void CmbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedDevice = cmbDevice.SelectedItem.ToString();
            bool needsFreeInput = selectedDevice == "Multi" || selectedDevice == "Autre";

            txtDeviceFree.Enabled = needsFreeInput;

            if (!needsFreeInput)
                txtDeviceFree.Text = "";
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                ValidateForm();

                DocGenerationRequest request = new DocGenerationRequest
                {
                    ProjectRoot = txtProjectPath.Text.Trim(),
                    DocumentType = cmbDocumentType.Text,
                    ShortTitle = txtShortTitle.Text.Trim(),
                    DocumentReference = txtDocumentReference.Text.Trim(),
                    Device = GetDeviceValue(),
                    Range = cmbRange.Text,
                    FullTitle = txtFullTitle.Text.Trim()
                };

                AtlasDocGenerationService service = new AtlasDocGenerationService();
                GenerationResult result = service.CreateDocumentFolder(request);

                string recap =
                    "Package documentaire créé avec succès.\n\n" +
                    "Dossier documentaire :\n" + result.DocumentFolderPath + "\n\n" +
                    "Topics créés : " + result.CreatedTopicPaths.Count + "\n\n" +
                    "TOC créée :\n" + result.TocPath + "\n\n" +
                    "Target créée :\n" + result.TargetPath + "\n\n" +
                    "Étape suivante : ouvrir MadCap et vérifier que la target générée pointe vers la bonne TOC.";
                MessageBox.Show(
                    recap,
                    "ATLAS Doc Generator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "ATLAS Doc Generator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtProjectPath.Text))
                throw new Exception("Le dossier du projet est obligatoire.");

            if (!Directory.Exists(txtProjectPath.Text))
                throw new Exception("Le dossier du projet sélectionné n'existe pas.");

            if (string.IsNullOrWhiteSpace(txtShortTitle.Text))
                throw new Exception("Le titre doc abrégé est obligatoire.");

            if (txtShortTitle.Text.Length > 40)
                throw new Exception("Le titre doc abrégé doit faire 35 caractères maximum.");

            if (ContainsForbiddenCharacters(txtShortTitle.Text))
                throw new Exception("Le titre doc abrégé contient des caractères interdits.");

            if (string.IsNullOrWhiteSpace(txtDocumentReference.Text))
                throw new Exception("La référence est obligatoire.");

            if (ContainsForbiddenCharacters(txtDocumentReference.Text))
                throw new Exception("La référence contient des caractères interdits.");

            if (string.IsNullOrWhiteSpace(GetDeviceValue()))
                throw new Exception("Le dispositif est obligatoire.");

            if (string.IsNullOrWhiteSpace(txtFullTitle.Text))
                throw new Exception("Le titre complet est obligatoire.");
        }

        private string GetDeviceValue()
        {
            string selectedDevice = cmbDevice.SelectedItem.ToString();

            if (selectedDevice == "Multi" || selectedDevice == "Autre")
                return txtDeviceFree.Text.Trim();

            return selectedDevice;
        }

        private bool ContainsForbiddenCharacters(string value)
        {
            if (value == null)
                return false;

            return Regex.IsMatch(value, @"[<>:""/\\|?*]");
        }

        private string ToSafeName(string value)
        {
            string cleaned = value.Trim();

            cleaned = cleaned.Replace(" ", "_");
            cleaned = cleaned.Replace("é", "e").Replace("è", "e").Replace("ê", "e");
            cleaned = cleaned.Replace("à", "a").Replace("â", "a");
            cleaned = cleaned.Replace("î", "i").Replace("ï", "i");
            cleaned = cleaned.Replace("ô", "o").Replace("ö", "o");
            cleaned = cleaned.Replace("ù", "u").Replace("û", "u");
            cleaned = cleaned.Replace("ç", "c");

            return cleaned;
        }
    }
}