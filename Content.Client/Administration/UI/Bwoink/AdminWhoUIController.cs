
using Content.Client.Administration.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Administration.UI.Bwoink
{
    [UsedImplicitly]
    public sealed class AdminWhoUIController : UIController, IOnSystemChanged<AdminWhoSystem>
    {
        private AdminWhoWindow? _dialog;
        private AdminWhoSystem? _adminWhoSystem;

        protected override string SawmillName => "c.c.admin.adminwho";

        public void OnSystemLoaded(AdminWhoSystem system)
        {
            _adminWhoSystem = system;

            _dialog = new AdminWhoWindow();
            _dialog.Initialize(system);
        }

        public void OnSystemUnloaded(AdminWhoSystem system)
        {
            if (_dialog != null)
            {
                _dialog.Close();
                _dialog.Uninitialize();
                _dialog.Dispose();
                _dialog = null;
            }

            _adminWhoSystem = null;
        }

        public void Open()
        {
            if (_dialog == null)
            {
                if (_adminWhoSystem == null)
                    return;

                _dialog = new AdminWhoWindow();
                _dialog.Initialize(_adminWhoSystem);
            }

            _dialog.OpenCentered();
            _dialog.RefreshAdminList();
        }

        public void Toggle()
        {
            if (_dialog?.IsOpen == true)
                Close();
            else
                Open();
        }

        public void Close()
        {
            _dialog?.Close();
        }
    }
}
