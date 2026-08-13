using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ATLASDocGenerator.Models;
using ATLASDocGenerator.Services.Checklist;

namespace ATLASDocGenerator.Forms
{
    public class ChecklistGeneratorForm : Form
    {
        private readonly string projectRoot;
        private ComboBox targetComboBox;
        private RadioButton appendRadioButton;
        private RadioButton newDocumentRadioButton;
        private Label referenceLabel;
        private TextBox referenceTextBox;
        private Button generateButton;

        public string GeneratedTopicPath { get; private set; }

        public ChecklistGeneratorForm(string projectRootPath)
        {
            projectRoot = projectRootPath;
            InitializeComponent();
            LoadTargets();
        }

        private void InitializeComponent()
        {
            Text = "Checklist Generator - ATLAS";
            Width = 680;
            Height = 335;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label documentLabel = new Label
            {
                Text = "Sélectionner le document :",
                Left = 20,
                Top = 28,
                Width = 180
            };
            targetComboBox = new ComboBox
            {
                Left = 205,
                Top = 24,
                Width = 430,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            appendRadioButton = new RadioButton
            {
                Text = "Ajouter la checklist à la fin de la TOC du document actuel",
                Left = 205,
                Top = 78,
                Width = 430,
                Checked = true
            };
            newDocumentRadioButton = new RadioButton
            {
                Text = "Créer un nouveau document checklist",
                Left = 205,
                Top = 112,
                Width = 430
            };
            newDocumentRadioButton.CheckedChanged += ModeChanged;

            referenceLabel = new Label
            {
                Text = "Document Reference :",
                Left = 20,
                Top = 161,
                Width = 180,
                Enabled = false
            };
            referenceTextBox = new TextBox
            {
                Left = 205,
                Top = 157,
                Width = 430,
                Enabled = false
            };

            Label helpLabel = new Label
            {
                Text = "Le nouveau document sera nommé <Document Reference>_checklist.",
                Left = 205,
                Top = 187,
                Width = 430,
                Height = 32
            };

            generateButton = new Button
            {
                Text = "Générer",
                Left = 445,
                Top = 235,
                Width = 90
            };
            generateButton.Click += GenerateButtonClick;

            Button cancelButton = new Button
            {
                Text = "Annuler",
                Left = 545,
                Top = 235,
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(documentLabel);
            Controls.Add(targetComboBox);
            Controls.Add(appendRadioButton);
            Controls.Add(newDocumentRadioButton);
            Controls.Add(referenceLabel);
            Controls.Add(referenceTextBox);
            Controls.Add(helpLabel);
            Controls.Add(generateButton);
            Controls.Add(cancelButton);
            AcceptButton = generateButton;
            CancelButton = cancelButton;
        }

        private void LoadTargets()
        {
            List<ChecklistTargetInfo> targets =
                new ChecklistGeneratorService().GetAvailableTargets(projectRoot);
            foreach (ChecklistTargetInfo target in targets)
                targetComboBox.Items.Add(target);

            if (targetComboBox.Items.Count == 0)
                throw new InvalidOperationException(
                    "Aucune target possédant une TOC valide n'a été trouvée dans Project/Targets et ses sous-dossiers.");
            targetComboBox.SelectedIndex = 0;
        }

        private void ModeChanged(object sender, EventArgs e)
        {
            referenceLabel.Enabled = newDocumentRadioButton.Checked;
            referenceTextBox.Enabled = newDocumentRadioButton.Checked;
        }

        private void GenerateButtonClick(object sender, EventArgs e)
        {
            try
            {
                ChecklistTargetInfo target = targetComboBox.SelectedItem as ChecklistTargetInfo;
                if (target == null)
                    throw new InvalidOperationException("Sélectionnez un document.");
                if (newDocumentRadioButton.Checked
                    && string.IsNullOrWhiteSpace(referenceTextBox.Text))
                {
                    throw new InvalidOperationException("Document Reference est obligatoire pour le nouveau document.");
                }

                ChecklistGenerationResult result =
                    new ChecklistGeneratorService().Generate(new ChecklistGenerationRequest
                    {
                        ProjectRoot = projectRoot,
                        SourceTargetPath = target.TargetPath,
                        CreateNewDocument = newDocumentRadioButton.Checked,
                        NewDocumentReference = referenceTextBox.Text.Trim()
                    });

                GeneratedTopicPath = result.ChecklistTopicPath;
                MessageBox.Show(
                    "Checklist créée avec succès.\n\nSections : " + result.SectionCount
                    + "\nTOC : " + result.TocPath
                    + (string.IsNullOrWhiteSpace(result.TargetPath) ? string.Empty : "\nTarget : " + result.TargetPath),
                    "ATLAS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ATLAS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
