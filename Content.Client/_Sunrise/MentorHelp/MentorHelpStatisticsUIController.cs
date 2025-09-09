using Content.Client.Administration.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.MentorHelp
{
    [UsedImplicitly]
    public sealed class MentorHelpStatisticsUIController : UIController, IOnSystemChanged<MentorHelpSystem>
    {
        private MentorHelpStatisticsDialog? _dialog;
        private MentorHelpSystem? _mentorHelpSystem;

        protected override string SawmillName => "c.s.go.es.mhelp.stats";

        public void OnSystemLoaded(MentorHelpSystem system)
        {
            _mentorHelpSystem = system;

            _dialog = new MentorHelpStatisticsDialog();
            _dialog.Initialize(system);
        }

        public void OnSystemUnloaded(MentorHelpSystem system)
        {
            if (_dialog != null)
            {
                _dialog.Close();
                _dialog.Uninitialize();
                _dialog = null;
            }

            _mentorHelpSystem = null;
        }

        public void OpenStatistics()
        {
            if (_dialog == null)
            {
                if (_mentorHelpSystem == null)
                    return;

                _dialog = new MentorHelpStatisticsDialog();
                _dialog.Initialize(_mentorHelpSystem);
            }

            _dialog.OpenCentered();
        }

        public void ToggleStatistics()
        {
            if (_dialog?.IsOpen == true)
            {
                CloseStatistics();
            }
            else
            {
                OpenStatistics();
            }
        }

        public void CloseStatistics()
        {
            _dialog?.Close();
        }
    }
}
