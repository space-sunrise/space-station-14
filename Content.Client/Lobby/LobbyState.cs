using System.Linq;
using Content.Client._Sunrise.Contributors;
using Content.Client._Sunrise.Latejoin;
using Content.Client._Sunrise.ServersHub;
using Content.Client.Audio;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Content.Shared.CCVar;
using Content.Shared._Sunrise.Contributors;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Client.Changelog;
using Content.Client.Parallax.Managers;
using Content.Server.GameTicking.Prototypes;
using Content.Shared._Sunrise.Lobby;
using Content.Shared._Sunrise.ServersHub;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Serilog;

namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
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
        [Dependency] private readonly ContributorsManager _contributorsManager = default!;
        [Dependency] private readonly ChangelogManager _changelogManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

            var lobbyNameCvar = _cfg.GetCVar(CCVars.ServerLobbyName);
            var serverName = _baseClient.GameInfo?.ServerName ?? string.Empty;

            // Lobby.ServerName.Text = string.IsNullOrEmpty(lobbyNameCvar)
            //     ? Loc.GetString("ui-lobby-title", ("serverName", serverName))
            //     : lobbyNameCvar;

            var width = _cfg.GetCVar(CCVars.ServerLobbyRightPanelWidth);
            Lobby.RightPanel.SetWidth = width;

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

            Lobby.LocalChangelogBody.PopulateChangelog(combinedChangelog);
            Lobby.LobbyAnimation.DisplayRect.Stretch = TextureRect.StretchMode.KeepAspectCovered;
            Lobby.LobbyAnimation.DisplayRect.HorizontalExpand = true;
            Lobby.LobbyAnimation.DisplayRect.VerticalExpand = true;


            _cfg.OnValueChanged(SunriseCCVars.LobbyBackgroundType, OnLobbyBackgroundTypeChanged, true);
            _cfg.OnValueChanged(SunriseCCVars.LobbyArt, OnLobbyArtChanged, true);
            _cfg.OnValueChanged(SunriseCCVars.LobbyAnimation, OnLobbyAnimationChanged, true);
            _cfg.OnValueChanged(SunriseCCVars.LobbyParallax, OnLobbyParallaxChanged, true);
            // Sunrise-end

            Lobby.CharacterPreview.CharacterSetupButton.OnPressed += OnSetupPressed;
            Lobby.ReadyButton.OnPressed += OnReadyPressed;
            Lobby.ReadyButton.OnToggled += OnReadyToggled;

            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;

            _serversHubManager.ServersDataListChanged += RefreshServersHubHeader;
            _contributorsManager.ContributorsDataListChanged += RefreshContributorsHeader;

            RefreshContributorsHeader(_contributorsManager.ContributorsDataList);
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
            _contributorsManager.ContributorsDataListChanged -= RefreshContributorsHeader;
        }

        private void RefreshServersHubHeader(List<ServerHubEntry> servers)
        {
            var totalPlayers = _serversHubManager.ServersDataList.Sum(server => server.CurrentPlayers);
            var maxPlayers = _serversHubManager.ServersDataList.Sum(server => server.MaxPlayers);
            Lobby!.ServersHubHeaderLabel.Text = Loc.GetString("serverhub-playingnow", ("total", totalPlayers), ("max", maxPlayers)); // Sunrise-Edit
        }

        private void RefreshContributorsHeader(List<ContributorEntry> contributors)
        {
            Lobby!.ContributorsHeaderLabel.Text = Loc.GetString("contributors-header-count", ("count", contributors.Count));
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

            Lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-not-started");
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
                else if (difference.TotalHours >= 1)
                {
                    text = $"{Math.Floor(difference.TotalHours)}:{difference.Minutes:D2}:{difference.Seconds:D2}";
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
            UpdateLobbyType();
            UpdateLobbyParallax();
            UpdateLobbyAnimation();
            UpdateLobbyArt();
            // Sunrise-End
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

        private void OnLobbyBackgroundTypeChanged(string lobbyBackgroundTypeString)
        {
            if (lobbyBackgroundTypeString == "Random" && _gameTicker.LobbyType != null)
                SetLobbyBackgroundType(_gameTicker.LobbyType);
            else
            {
                SetLobbyBackgroundType(lobbyBackgroundTypeString);
            }
        }

        public void SetLobbyBackgroundType(string lobbyBackgroundString)
        {
            if (!Enum.TryParse(lobbyBackgroundString, out LobbyBackgroundType lobbyBackgroundTypeString))
            {
                lobbyBackgroundTypeString = default;
            }

            if (Lobby == null)
            {
                Logger.Error("Error in SetLobbyBackgroundType. Lobby is null");
                return;
            }

            switch (lobbyBackgroundTypeString)
            {
                case LobbyBackgroundType.Parallax:
                    Lobby!.LobbyAnimation.Visible = false;
                    Lobby!.LobbyArt.Visible = false;
                    Lobby!.ShowParallax = true;
                    break;
                case LobbyBackgroundType.Art:
                    Lobby!.LobbyAnimation.Visible = false;
                    Lobby!.LobbyArt.Visible = true;
                    Lobby!.ShowParallax = false;
                    break;
                case LobbyBackgroundType.Animation:
                    Lobby!.LobbyAnimation.Visible = true;
                    Lobby!.LobbyArt.Visible = false;
                    Lobby!.ShowParallax = false;
                    break;
            }
        }

        private void OnLobbyArtChanged(string lobbyArt)
        {
            if (lobbyArt == "Random" && _gameTicker.LobbyArt != null)
                SetLobbyArt(_gameTicker.LobbyArt);
            else
            {
                SetLobbyArt(lobbyArt);
            }
        }

        private void OnLobbyAnimationChanged(string lobbyAnimation)
        {
            if (lobbyAnimation == "Random" && _gameTicker.LobbyAnimation != null)
                SetLobbyAnimation(_gameTicker.LobbyAnimation);
            else
            {
                SetLobbyAnimation(lobbyAnimation);
            }
        }

        private void OnLobbyParallaxChanged(string lobbyParallax)
        {
            if (lobbyParallax == "Random" && _gameTicker.LobbyParallax != null)
                SetLobbyParallax(_gameTicker.LobbyParallax);
            else
            {
                SetLobbyParallax(lobbyParallax);
            }
        }

        private void SetLobbyAnimation(string lobbyAnimation)
        {
            if (!_prototypeManager.TryIndex<LobbyAnimationPrototype>(lobbyAnimation, out var lobbyAnimationPrototype))
                return;

            if (Lobby == null)
            {
                Logger.Error("Error in SetLobbyAnimation. Lobby is null");
                return;
            }

            Lobby!.LobbyAnimation.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(new ResPath(lobbyAnimationPrototype.RawPath), lobbyAnimationPrototype.State));
            Lobby!.LobbyAnimation.DisplayRect.TextureScale = lobbyAnimationPrototype.Scale;
        }

        private void SetLobbyArt(string lobbyArt)
        {
            if (!_prototypeManager.TryIndex<LobbyBackgroundPrototype>(lobbyArt, out var lobbyArtPrototype))
                return;

            if (Lobby == null)
            {
                Logger.Error("Error in SetLobbyArt. Lobby is null");
                return;
            }

            Lobby!.LobbyArt.Texture = _resourceCache.GetResource<TextureResource>(lobbyArtPrototype.Background);
        }

        private void SetLobbyParallax(string lobbyParallax)
        {
            if (!_prototypeManager.TryIndex<LobbyParallaxPrototype>(lobbyParallax, out var lobbyParallaxPrototype))
                return;

            if (Lobby == null)
            {
                Logger.Error("Error in SetLobbyParallax. Lobby is null");
                return;
            }

            _parallaxManager.LoadParallaxByName(lobbyParallaxPrototype.Parallax);
            Lobby!.LobbyParallax = lobbyParallaxPrototype.Parallax;
        }

        private void UpdateLobbyType()
        {
            if (_cfg.GetCVar(SunriseCCVars.LobbyBackgroundType) != "Random")
                return;

            SetLobbyBackgroundType(_gameTicker.LobbyType!);
        }

        private void UpdateLobbyAnimation()
        {
            if (_cfg.GetCVar(SunriseCCVars.LobbyAnimation) != "Random")
                return;

            SetLobbyAnimation(_gameTicker.LobbyAnimation!);
        }

        private void UpdateLobbyArt()
        {
            if (_cfg.GetCVar(SunriseCCVars.LobbyArt) != "Random")
                return;

            SetLobbyArt(_gameTicker.LobbyArt!);
        }

        private void UpdateLobbyParallax()
        {
            if (_cfg.GetCVar(SunriseCCVars.LobbyParallax) != "Random")
                return;

            SetLobbyParallax(_gameTicker.LobbyParallax!);
        }

        // Sunrise-end

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
