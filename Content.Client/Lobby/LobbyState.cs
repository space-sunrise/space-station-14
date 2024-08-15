using System.Linq;
using Content.Client._Sunrise.Latejoin;
using Content.Client._Sunrise.ServersHub;
using Content.Client.Audio;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Client.Changelog;
using Content.Client.Parallax.Managers;
using Content.Shared._Sunrise.ServersHub;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;


namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;
        [Dependency] private readonly IResourceManager _resource = default!;
        [Dependency] private readonly ServersHubManager _serversHubManager = default!;
        [Dependency] private readonly ChangelogManager _changelogManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private ClientGameTicker _gameTicker = default!;
        private ContentAudioSystem _contentAudioSystem = default!;

        protected override Type? LinkedScreenType { get; } = typeof(LobbyGui);
        public LobbyGui? Lobby;

        protected override void Startup()
        {
            if (_userInterfaceManager.ActiveScreen == null)
            {
                return;
            }

            Lobby = (LobbyGui) _userInterfaceManager.ActiveScreen;

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            _gameTicker = _entityManager.System<ClientGameTicker>();
            _contentAudioSystem = _entityManager.System<ContentAudioSystem>();
            _contentAudioSystem.LobbySoundtrackChanged += UpdateLobbySoundtrackInfo;

            chatController.SetMainChat(true);

            _voteManager.SetPopupContainer(Lobby.VoteContainer);
            LayoutContainer.SetAnchorPreset(Lobby, LayoutContainer.LayoutPreset.Wide);
            // Sunrise-start
            //Lobby.ServerName.Text = _baseClient.GameInfo?.ServerName; //The eye of refactor gazes upon you...
            UpdateLobbyUi();

            Lobby!.LocalChangelogBody.CleanChangelog();

            var lobbyChangelogs = _cfg.GetCVar(SunriseCCVars.LobbyChangelogsList).Split(',');

            var changelogs = new List<ChangelogManager.Changelog>();
            foreach (var lobbyChangelog in lobbyChangelogs)
            {
                var yamlData = _resource.ContentFileReadYaml(new ResPath($"/Changelog/{lobbyChangelog}"));

                var node = yamlData.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
                var changelog = _serialization.Read<ChangelogManager.Changelog>(node, notNullableOverride: true);
                changelogs.Add(changelog);
            }
            var combinedChangelog = _changelogManager.MergeChangelogs(changelogs);

            Lobby!.LocalChangelogBody.PopulateChangelog(combinedChangelog);

            // Sunrise-end

            Lobby.CharacterPreview.CharacterSetupButton.OnPressed += OnSetupPressed;
            Lobby.ReadyButton.OnPressed += OnReadyPressed;
            Lobby.ReadyButton.OnToggled += OnReadyToggled;

            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;

            _serversHubManager.ServersDataListChanged += RefreshServersHubHeader;
        }

        protected override void Shutdown()
        {
            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            chatController.SetMainChat(false);
            _gameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;
            _contentAudioSystem.LobbySoundtrackChanged -= UpdateLobbySoundtrackInfo;

            _voteManager.ClearPopupContainer();

            Lobby!.CharacterPreview.CharacterSetupButton.OnPressed -= OnSetupPressed;
            Lobby!.ReadyButton.OnPressed -= OnReadyPressed;
            Lobby!.ReadyButton.OnToggled -= OnReadyToggled;

            Lobby = null;

            _serversHubManager.ServersDataListChanged -= RefreshServersHubHeader;
        }

        private void RefreshServersHubHeader(List<ServerHubEntry> servers)
        {
            var totalPlayers = _serversHubManager.ServersDataList.Sum(server => server.CurrentPlayers);
            var maxPlayers = _serversHubManager.ServersDataList.Sum(server => server.MaxPlayers);
            Lobby!.ServersHubHeaderLabel.Text = Loc.GetString("serverhub-playingnow", ("total", totalPlayers), ("max", maxPlayers)); // Sunrise-Edit
        }

        public void SwitchState(LobbyGui.LobbyGuiState state)
        {
            // Yeah I hate this but LobbyState contains all the badness for now.
            Lobby?.SwitchState(state);
        }

        private void OnSetupPressed(BaseButton.ButtonEventArgs args)
        {
            SetReady(false);
            Lobby?.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        }

        private void OnReadyPressed(BaseButton.ButtonEventArgs args)
        {
            if (!_gameTicker.IsGameStarted)
            {
                return;
            }

            new SRLateJoinGui().OpenCentered(); // Sunrise-Edit
        }

        private void OnReadyToggled(BaseButton.ButtonToggledEventArgs args)
        {
            SetReady(args.Pressed);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                var roundTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                Lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-time", ("hours", roundTime.Hours), ("minutes", roundTime.Minutes));
                return;
            }

            Lobby!.StationTime.Text =  Loc.GetString("lobby-state-player-status-round-not-started");
            string text;

            if (_gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else if (_gameTicker.StartTime < _gameTiming.CurTime)
            {
                Lobby!.StationTime.Text = Loc.GetString("lobby-state-soon");
                return;
            }
            else
            {
                var difference = _gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5 ? "lobby-state-right-now-question" : "lobby-state-right-now-confirmation");
                }
                else
                {
                    text = $"{difference.Minutes}:{difference.Seconds:D2}";
                }
            }

            Lobby!.StationTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            // Sunrise-Start
            UpdateLobbyaralax();
            UpdateLobbyImage();
            // Sunrise-End
            UpdateLobbyBackground();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            Lobby!.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                Lobby!.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-join-state");
                Lobby!.ReadyButton.ToggleMode = false;
                Lobby!.ReadyButton.Pressed = false;
                Lobby!.ObserveButton.Disabled = false;
                Lobby!.GhostRolesButton.Disabled = false;
            }
            else
            {
                Lobby!.ReadyButton.Text = Loc.GetString(Lobby!.ReadyButton.Pressed ? "lobby-state-player-status-ready": "lobby-state-player-status-not-ready");
                Lobby!.ReadyButton.ToggleMode = true;
                Lobby!.ReadyButton.Disabled = false;
                Lobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                Lobby!.ObserveButton.Disabled = true;
                Lobby!.GhostRolesButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                Lobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }
        }

        private void UpdateLobbySoundtrackInfo(LobbySoundtrackChangedEvent ev)
        {
            if (ev.SoundtrackFilename == null)
            {
                Lobby!.LobbySong.SetMarkup(Loc.GetString("lobby-state-song-no-song-text"));
            }
            else if (
                ev.SoundtrackFilename != null
                && _resourceCache.TryGetResource<AudioResource>(ev.SoundtrackFilename, out var lobbySongResource)
                )
            {
                var lobbyStream = lobbySongResource.AudioStream;

                var title = string.IsNullOrEmpty(lobbyStream.Title)
                    ? Loc.GetString("lobby-state-song-unknown-title")
                    : lobbyStream.Title;

                var artist = string.IsNullOrEmpty(lobbyStream.Artist)
                    ? Loc.GetString("lobby-state-song-unknown-artist")
                    : lobbyStream.Artist;

                var markup = Loc.GetString("lobby-state-song-text",
                    ("songTitle", title),
                    ("songArtist", artist));

                Lobby!.LobbySong.SetMarkup(markup);
            }
        }

        // Sunrise-start
        private void UpdateLobbyaralax()
        {
            if (_gameTicker.LobbyParalax != null)
            {
                _parallaxManager.LoadParallaxByName(_gameTicker.LobbyParalax);
                Lobby!.LobbyParalax = _gameTicker.LobbyParalax;
            }
            else
            {
                Lobby!.LobbyParalax = "FastSpace";
            }
        }

        private void UpdateLobbyImage()
        {
            if (_gameTicker.LobbyImage == null)
                return;

            Lobby!.LobbyImage.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(new ResPath(_gameTicker.LobbyImage.Path), _gameTicker.LobbyImage.State));
            Lobby!.LobbyImage.DisplayRect.TextureScale = _gameTicker.LobbyImage.Scale;
        }
        // Sunrise-end

        private void UpdateLobbyBackground()
        {
            if (_gameTicker.LobbyBackground != null)
            {
                Lobby!.Background.Texture = _resourceCache.GetResource<TextureResource>(_gameTicker.LobbyBackground );
            }
            else
            {
                Lobby!.Background.Texture = null;
            }

        }

        private void SetReady(bool newReady)
        {
            if (_gameTicker.IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
        }
    }
}

public enum LobbyBackgroundType
{
    Paralax,
    Art
}
