using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ATLASDocGenerator.Models.AitImportFinalizer;
using ATLASDocGenerator.Services.AitImportFinalizer;

namespace ATLASDocGenerator.Forms
{
    public class AitImportFinalizerForm : Form
    {
        private ComboBox documentTypeComboBox;
        private TextBox titleTextBox;
        private TextBox deviceTextBox;
        private TextBox referenceTextBox;
        private TextBox indexTextBox;
        private TextBox languageTextBox;
        private TextBox mrefTextBox;

        private CheckBox copyResourcesCheckBox;
        private CheckBox cleanTocCheckBox;
        private CheckBox updateVariablesCheckBox;
        private CheckBox configureTargetCheckBox;
        private CheckBox runCleanupCheckBox;

        private Button runButton;
        private Button cancelButton;

        public AitImportFinalizerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "AIT Import Finalizer";
            Width = 620;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;

            Label titleLabel = new Label
            {
                Text = "Finaliser un import Author-it vers MadCap",
                Left = 20,
                Top = 20,
                Width = 560,
                Height = 25,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            Label docTypeLabel = new Label
            {
                Text = "Type de document :",
                Left = 20,
                Top = 65,
                Width = 160
            };

            documentTypeComboBox = new ComboBox
            {
                Left = 190,
                Top = 60,
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            documentTypeComboBox.Items.Add(new ComboItem("Bulletin Technique", AitDocumentType.TechnicalBulletin));
            documentTypeComboBox.Items.Add(new ComboItem("Notice utilisateur", AitDocumentType.UserNotice));
            documentTypeComboBox.Items.Add(new ComboItem("Addenda", AitDocumentType.Addenda));
            documentTypeComboBox.Items.Add(new ComboItem("Manuel de référence / MRef", AitDocumentType.ReferenceManual));
            documentTypeComboBox.Items.Add(new ComboItem("Document technique", AitDocumentType.TechnicalDocument));
            documentTypeComboBox.Items.Add(new ComboItem("Document technique multi-instrument", AitDocumentType.MultiInstrumentTechnicalDocument));
            documentTypeComboBox.SelectedIndex = 0;

            int top = 105;

            titleTextBox = AddTextField("Titre document :", top);
            top += 35;

            deviceTextBox = AddTextField("Dispositif :", top);
            top += 35;

            referenceTextBox = AddTextField("Référence :", top);
            top += 35;

            indexTextBox = AddTextField("Indice :", top);
            top += 35;

            languageTextBox = AddTextField("Langue :", top);
            languageTextBox.Text = "FR";
            top += 35;

            mrefTextBox = AddTextField("Réf. MRef :", top);
            top += 45;

            copyResourcesCheckBox = AddCheckBox("Copier les ressources Stago", top, true);
            top += 28;

            cleanTocCheckBox = AddCheckBox("Nettoyer la TOC", top, true);
            top += 28;

            updateVariablesCheckBox = AddCheckBox("Mettre à jour les variables", top, true);
            top += 28;

            configureTargetCheckBox = AddCheckBox("Configurer la target PDF", top, true);
            top += 28;

            runCleanupCheckBox = AddCheckBox("Lancer AIT Cleanup", top, true);
            top += 45;

            runButton = new Button
            {
                Text = "Lancer",
                Left = 340,
                Top = top,
                Width = 95
            };
            runButton.Click += RunButton_Click;

            cancelButton = new Button
            {
                Text = "Annuler",
                Left = 445,
                Top = top,
                Width = 95
            };
            cancelButton.Click += (sender, args) => Close();

            Controls.Add(titleLabel);
            Controls.Add(docTypeLabel);
            Controls.Add(documentTypeComboBox);
            Controls.Add(runButton);
            Controls.Add(cancelButton);
        }

        private TextBox AddTextField(string label, int top)
        {
            Label fieldLabel = new Label
            {
                Text = label,
                Left = 20,
                Top = top + 4,
                Width = 160
            };

            TextBox textBox = new TextBox
            {
                Left = 190,
                Top = top,
                Width = 350
            };

            Controls.Add(fieldLabel);
            Controls.Add(textBox);

            return textBox;
        }

        private CheckBox AddCheckBox(string text, int top, bool isChecked)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text,
                Left = 190,
                Top = top,
                Width = 350,
                Checked = isChecked
            };

            Controls.Add(checkBox);

            return checkBox;
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            ComboItem selectedItem = documentTypeComboBox.SelectedItem as ComboItem;

            if (selectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un type de document.", "AIT Import Finalizer");
                return;
            }

            AitImportFinalizerOptions options = new AitImportFinalizerOptions
            {
                DocumentType = selectedItem.DocumentType,
                DocumentTitle = titleTextBox.Text,
                DeviceName = deviceTextBox.Text,
                DocumentReference = referenceTextBox.Text,
                DocumentIndex = indexTextBox.Text,
                Language = languageTextBox.Text,
                MrefReference = mrefTextBox.Text,
                CopyResources = copyResourcesCheckBox.Checked,
                CleanToc = cleanTocCheckBox.Checked,
                UpdateVariables = updateVariablesCheckBox.Checked,
                ConfigureTarget = configureTargetCheckBox.Checked,
                RunCleanup = runCleanupCheckBox.Checked
            };

            AitImportFinalizerService service = new AitImportFinalizerService();
            AitImportFinalizerReport report = service.Run(options);

            MessageBox.Show(BuildReportMessage(report), "AIT Import Finalizer");
        }

        private string BuildReportMessage(AitImportFinalizerReport report)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("AIT Import Finalizer");
            builder.AppendLine();
            builder.AppendLine("Profile: " + report.ProfileName);
            builder.AppendLine();

            if (report.Warnings.Count > 0)
            {
                builder.AppendLine("Warnings:");
                foreach (string warning in report.Warnings)
                {
                    builder.AppendLine("- " + warning);
                }
            }

            if (report.Errors.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Errors:");
                foreach (string error in report.Errors)
                {
                    builder.AppendLine("- " + error);
                }
            }

            return builder.ToString();
        }

        private class ComboItem
        {
            public string Text { get; private set; }

            public AitDocumentType DocumentType { get; private set; }

            public ComboItem(string text, AitDocumentType documentType)
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