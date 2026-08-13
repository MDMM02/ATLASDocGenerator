using System;
using System.Windows.Forms;
using B3.PluginAPIKit;
using ATLASDocGenerator.Commands;
using ATLASDocGenerator.Forms;
using ATLASDocGenerator.Services;
using ATLASDocGenerator.Services.Checklist;

namespace ATLASDocGenerator
{
    /// <summary>
    /// 
    /// Entrée principale du plugin MyFlarePlugin.
    /// Classe chargée par MadCap Flare grâce à son API de plug-in.
    /// 
    /// Responsabilités :
    /// - mémoriser l'instance hôte de Flare ; 
    /// - récupérer les contextes d'édition ou de navigation de Flare ; 
    /// - créer l'onglet du ruban et les boutons ATLAS ; 
    /// - ouvrir les formulaires appropriés ou lancer des commandes de haut niveau.
    /// 
    /// Toute la logique métier est déléguée à des services spécialisés (Forms, Commands, Services).
    /// 
    /// </summary>
    public class MyFlarePlugin : IPlugin
    {
        private IHost _host;
        private IEditorContext _editorContext;
        private INavContext _navContext;
        private bool _activated;

        // Indique si le plugin est activé ou non. Permet de ne pas exécuter certaines actions si le plugin est désactivé.
        public bool IsActivated
        {
            get { return _activated; }
        }

        public string GetName()
        {
            return "ATLAS";
        }

        public string GetVersion()
        {
            return "0.1";
        }

        public string GetAuthor()
        {
            return "M. Michot";
        }

        public string GetDescription()
        {
            return "Adds an ATLAS tab and opens ATLAS documentation tools.";
        }

        // Appelé par Flare pour initialiser le plugin. On y récupère l'instance hôte de Flare et on crée l'onglet du ruban.
        // La récuperation du contexte s'effectue dans Execute().
        public void Initialize(IHost host)
        {
            _host = host;
        }

        // Appelé par Flare qd l'utilisateur active le pluin.
        // Récupère le contexte Flare et crée l'onglet du ruban et les boutons ATLAS.
        public void Execute()
        {
            try
            {
                _editorContext = _host.GetEditorContext();
                _navContext = _host.GetNavContext();

                CreateAtlasRibbon();

                _activated = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'activation du plugin ATLAS :\n\n" + ex,
                    "ATLAS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        public void Stop()
        {
            try
            {
                if (_host != null)
                {
                    _host.Dispose();
                }
            }
            finally
            {
                _activated = false;
            }
        }
        // Crée l'onglet du ruban ATLAS et les points d'entrée du plugin.
        // Associe les boutons de l'interface utilisateur aux commandes correspondantes.
        // La logique reste dans les formulaires et les services.
        private void CreateAtlasRibbon()
        {
            IRibbon ribbon = _navContext.GetRibbon();

            IRibbonTab atlasTab = ribbon.AddNewRibbonTab("ATLAS", "A");

            IRibbonGroup docGroup = atlasTab.AddNewRibbonGroup("Documentation");

            docGroup.AddRibbonButton(
                "Doc Generator",
                new RelayCommand(OpenDocGeneratorPopup),
                null,
                null,
                RibbonIconSize.Collapsed,
                "Doc Generator",
                "Ouvrir le Doc Generator d'ATLAS",
                "D"
            );

            IRibbonGroup checklistGroup = atlasTab.AddNewRibbonGroup("Checklist");

            checklistGroup.AddRibbonButton(
                "Generate Checklist",
                new RelayCommand(GenerateChecklist),
                null,
                null,
                RibbonIconSize.Collapsed,
                "Generate Checklist",
                "Generate a checklist from H1 sections in the active topic.",
                "G"
            );

            IRibbonGroup authorItGroup = atlasTab.AddNewRibbonGroup("Author-it");

            authorItGroup.AddRibbonButton(
                "Finaliser import AIT",
                new RelayCommand(OpenAitWorkflowPopup),
                null,
                null,
                RibbonIconSize.Collapsed,
                "Finaliser import AIT",
                "Installer les ressources, nettoyer l'import et vérifier la target.",
                "C"
            );
        }

        private void OpenAitWorkflowPopup(object parameter)
        {
            try
            {
                if (_editorContext == null || _editorContext.GetActiveDocument() == null)
                    throw new InvalidOperationException(
                        "Ouvrez un topic du projet Flare avant de finaliser un import AIT.");

                string projectRoot = FlareProjectContextService.ResolveProjectRoot(
                    _editorContext.GetActiveDocument());
                Form parentForm = _navContext.GetParentForm();
                using (AitWorkflowForm form = new AitWorkflowForm(projectRoot))
                    form.ShowDialog(parentForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'ouverture de la finalisation AIT :\n\n" + ex.Message,
                    "Finaliser import AIT",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OpenDocGeneratorPopup(object parameter)
        {
            try
            {
                if (_editorContext == null || _editorContext.GetActiveDocument() == null)
                {
                    throw new InvalidOperationException(
                        "Ouvrez un topic du projet Flare avant de lancer le Doc Generator.");
                }

                string projectRoot = FlareProjectContextService.ResolveProjectRoot(
                    _editorContext.GetActiveDocument());
                Form parentForm = _navContext.GetParentForm();

                using (DocGeneratorForm form = new DocGeneratorForm(projectRoot))
                {
                    form.ShowDialog(parentForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'ouverture du Doc Generator :\n\n" + ex,
                    "ATLAS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void OpenAitCleanupPopup(object parameter)
        {
            try
            {
                Form parentForm = _navContext.GetParentForm();

                using (AitCleanupForm form = new AitCleanupForm())
                {
                    form.ShowDialog(parentForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'ouverture du AIT Cleanup :\n\n" + ex,
                    "AIT Cleanup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void OpenAitImportFinalizerPopup(object parameter)
        {
            try
            {
                Form parentForm = _navContext.GetParentForm();

                using (AitImportFinalizerForm form = new AitImportFinalizerForm())
                {
                    form.ShowDialog(parentForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'ouverture du AIT Import Finalizer :\n\n" + ex,
                    "AIT Import Finalizer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void GenerateChecklist(object parameter)
        {
            try
            {
                if (_editorContext == null || _editorContext.GetActiveDocument() == null)
                    throw new InvalidOperationException(
                        "Ouvrez un topic du projet Flare avant de lancer le Checklist Generator.");

                string projectRoot = FlareProjectContextService.ResolveProjectRoot(
                    _editorContext.GetActiveDocument());
                Form parentForm = _navContext.GetParentForm();
                using (ChecklistGeneratorForm form = new ChecklistGeneratorForm(projectRoot))
                {
                    if (form.ShowDialog(parentForm) == DialogResult.OK
                        && !string.IsNullOrWhiteSpace(form.GeneratedTopicPath))
                    {
                        _editorContext.OpenDocument(form.GeneratedTopicPath, EditorView.Xml);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'ouverture du Checklist Generator :\n\n" + ex.Message,
                    "ATLAS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
