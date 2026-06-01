using System;
using System.Windows.Forms;
using B3.PluginAPIKit;
using ATLASDocGenerator.Commands;
using ATLASDocGenerator.Forms;

namespace ATLASDocGenerator
{
    public class MyFlarePlugin : IPlugin
    {
        private IHost _host;
        private IEditorContext _editorContext;
        private INavContext _navContext;
        private bool _activated;

        public bool IsActivated
        {
            get { return _activated; }
        }

        public string GetName()
        {
            return "ATLAS Doc Generator";
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
            return "Adds an ATLAS tab and opens the Doc Generator popup.";
        }

        public void Initialize(IHost host)
        {
            _host = host;
        }

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
                    "ATLAS Doc Generator",
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
                    _host.Dispose();
            }
            finally
            {
                _activated = false;
            }
        }

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
                "Open ATLAS Doc Generator",
                "D"
            );
        }

        private void OpenDocGeneratorPopup(object parameter)
        {
            try
            {
                Form parentForm = _navContext.GetParentForm();

                using (DocGeneratorForm form = new DocGeneratorForm())
                {
                    form.ShowDialog(parentForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant l'ouverture du Doc Generator :\n\n" + ex,
                    "ATLAS Doc Generator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}