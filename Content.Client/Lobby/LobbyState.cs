using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Client._Sunrise;
using Content.Client._Sunrise.Contributors;
using Content.Client._Sunrise.Latejoin;
using Content.Client._Sunrise.ServersHub;
using Content.Client.Audio;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Playtime;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Content.Shared.CCVar;
using Content.Shared._Sunrise.Contributors;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.Upload;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;
using System.Threading.Tasks;
using Content.Client.Changelog;
using Content.Client.Parallax.Managers;
using Content.Shared._Sunrise.Lobby;
using Content.Shared._Sunrise.ServersHub;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.GameTicking;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Content.Shared.GameTicking.Prototypes;

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
        [Dependency] private readonly ClientsidePlaytimeTrackingManager _playtimeTracking = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;
        [Dependency] private readonly IResourceManager _resource = default!;
        [Dependency] private readonly ServersHubManager _serversHubManager = default!;
        [Dependency] private readonly ContributorsManager _contributorsManager = default!;
        [Dependency] private readonly ChangelogManager _changelogManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly NetTexturesManager _netTexturesManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private ClientGameTicker _gameTicker = default!;
        private ContentAudioSystem _contentAudioSystem = default!;
        private ISawmill _sawmill = default!;

        // Track loaded resources for unloading
        private ResPath? _currentAnimationPath;
        private ResPath? _currentArtPath;

        private const string LoadingRsiPath = "/Textures/_Sunrise/loading.rsi";
        private const string LoadingState = "loading";

        protected override Type? LinkedScreenType { get; } = typeof(LobbyGui);
        public LobbyGui? Lobby;

        protected override void Startup()
        {
            _sawmill = _logManager.GetSawmill("lobby");

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

            // Sunrise-Start
            //var lobbyNameCvar = _cfg.GetCVar(CCVars.ServerLobbyName);
            //var serverName = _baseClient.GameInfo?.ServerName ?? string.Empty;

            // Lobby.ServerName.Text = string.IsNullOrEmpty(lobbyNameCvar)
            //     ? Loc.GetString("ui-lobby-title", ("serverName", serverName))
            //     : lobbyNameCvar;

            // var width = _cfg.GetCVar(CCVars.ServerLobbyRightPanelWidth);
            // Lobby.RightPanel.SetWidth = width;

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

            // Setup loading animation
            Lobby.LoadingAnimation.DisplayRect.Stretch = TextureRect.StretchMode.KeepAspectCentered;
            Lobby.LoadingAnimation.DisplayRect.TextureScale = new Vector2(2.0f, 2.0f);
            Lobby.LoadingAnimation.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(new ResPath(LoadingRsiPath), LoadingState));
            Lobby.LoadingAnimationContainer.Visible = false;


            _cfg.OnValueChanged(SunriseCCVars.LobbyBackgroundType, OnLobbyBackgroundTypeChanged, true);
            _cfg.OnValueChanged(SunriseCCVars.LobbyArt, OnLobbyArtChanged, true);
            _cfg.OnValueChanged(SunriseCCVars.LobbyAnimation, OnLobbyAnimationChanged, true);
            _cfg.OnValueChanged(SunriseCCVars.LobbyParallax, OnLobbyParallaxChanged, true);

            // Subscribe to resource loaded events
            _netTexturesManager.ResourceLoaded += OnNetworkResourceLoaded;
            // Sunrise-End

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

            // Unload lobby resources if CVar is enabled
            if (_cfg.GetCVar(SunriseCCVars.LobbyUnloadResources))
            {
                if (_currentAnimationPath.HasValue)
                {
                    UnloadResource(_currentAnimationPath.Value);
                    _currentAnimationPath = null;
                }
                if (_currentArtPath.HasValue)
                {
                    UnloadResource(_currentArtPath.Value);
                    _currentArtPath = null;
                }
            }

            Lobby = null;

            _serversHubManager.ServersDataListChanged -= RefreshServersHubHeader;
            _contributorsManager.ContributorsDataListChanged -= RefreshContributorsHeader;

            // Unsubscribe from resource loaded events
            _netTexturesManager.ResourceLoaded -= OnNetworkResourceLoaded;
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
            // Only update the selected background type, not all of them
            var backgroundType = _cfg.GetCVar(SunriseCCVars.LobbyBackgroundType);
            if (backgroundType == "Random" && _gameTicker.LobbyType != null)
            {
                backgroundType = _gameTicker.LobbyType;
            }

            if (!Enum.TryParse(backgroundType, out LobbyBackgroundType lobbyBackgroundType))
            {
                lobbyBackgroundType = LobbyBackgroundType.Parallax; // Default
            }

            switch (lobbyBackgroundType)
            {
                case LobbyBackgroundType.Parallax:
                    UpdateLobbyParallax();
                    break;
                case LobbyBackgroundType.Art:
                    UpdateLobbyArt();
                    break;
                case LobbyBackgroundType.Animation:
                    UpdateLobbyAnimation();
                    break;
            }
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
                //Lobby!.StartTime.Text = string.Empty;
                Lobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                Lobby!.ReadyButton.Text = Loc.GetString(Lobby!.ReadyButton.Pressed ? "lobby-state-player-status-ready": "lobby-state-player-status-not-ready");
                Lobby!.ReadyButton.ToggleMode = true;
                Lobby!.ReadyButton.Disabled = false;
                Lobby!.ObserveButton.Disabled = true;
                Lobby!.GhostRolesButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                Lobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }

            var minutesToday = _playtimeTracking.PlaytimeMinutesToday;
            if (minutesToday > 60)
            {
                Lobby!.PlaytimeComment.Visible = true;

                var hoursToday = Math.Round(minutesToday / 60f, 1);

                var chosenString = minutesToday switch
                {
                    < 180 => "lobby-state-playtime-comment-normal",
                    < 360 => "lobby-state-playtime-comment-concerning",
                    < 720 => "lobby-state-playtime-comment-grasstouchless",
                    _ => "lobby-state-playtime-comment-selfdestructive"
                };

                Lobby.PlaytimeComment.SetMarkup(Loc.GetString(chosenString, ("hours", hoursToday)));
            }
            else
                Lobby!.PlaytimeComment.Visible = false;
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
                _sawmill.Error("Error in SetLobbyBackgroundType. Lobby is null");
                return;
            }

            switch (lobbyBackgroundTypeString)
            {
                case LobbyBackgroundType.Parallax:
                    Lobby!.LobbyAnimation.Visible = false;
                    Lobby!.LobbyArt.Visible = false;
                    Lobby!.ShowParallax = true;
                    // Load parallax background
                    UpdateLobbyParallax();
                    break;
                case LobbyBackgroundType.Art:
                    Lobby!.LobbyAnimation.Visible = false;
                    Lobby!.LobbyArt.Visible = true;
                    Lobby!.ShowParallax = false;
                    // Load art background
                    UpdateLobbyArt();
                    break;
                case LobbyBackgroundType.Animation:
                    Lobby!.LobbyAnimation.Visible = true;
                    Lobby!.LobbyArt.Visible = false;
                    Lobby!.ShowParallax = false;
                    // Load animation background
                    UpdateLobbyAnimation();
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
            // Check if animation background type is currently selected
            var backgroundType = _cfg.GetCVar(SunriseCCVars.LobbyBackgroundType);
            if (backgroundType == "Random" && _gameTicker.LobbyType != null)
            {
                backgroundType = _gameTicker.LobbyType;
            }

            if (!Enum.TryParse(backgroundType, out LobbyBackgroundType lobbyBackgroundType) ||
                lobbyBackgroundType != LobbyBackgroundType.Animation)
            {
                // Animation is not the selected background type, don't load it
                return;
            }

            if (!_protoMan.TryIndex<LobbyAnimationPrototype>(lobbyAnimation, out var lobbyAnimationPrototype))
                return;

            if (Lobby == null)
            {
                _sawmill.Error("Error in SetLobbyAnimation. Lobby is null");
                return;
            }

            // Hide old animation and show loading animation immediately (before any resource checks)
            Lobby!.LobbyAnimation.Visible = false;
            ShowLoadingAnimation();

            // Unload previous animation if CVar is enabled
            if (_cfg.GetCVar(SunriseCCVars.LobbyUnloadResources) && _currentAnimationPath.HasValue)
            {
                UnloadResource(_currentAnimationPath.Value);
            }

            var rsiPath = lobbyAnimationPrototype.Animation;

            // Ensure the path ends with .rsi for RSI resources
            if (!rsiPath.EndsWith(".rsi") && !rsiPath.EndsWith(".rsi/"))
            {
                _sawmill.Warning($"Invalid RSI path format: {rsiPath}. Expected path ending with .rsi");
                return;
            }

            // Check if resource is available, request if not
            var isAvailable = _netTexturesManager.EnsureResource(rsiPath);

            ResPath targetPath;
            if (isAvailable)
            {
                // Resource is available, use uploaded path
                targetPath = _netTexturesManager.GetUploadedPath(rsiPath);
            }
            else
            {
                // Resource is being requested, try to use uploaded path first
                var uploadedPath = _netTexturesManager.GetUploadedPath(rsiPath);
                var metaPath = (uploadedPath / "meta.json").ToRootedPath();

                // Check if uploaded resource exists
                if (_resource.ContentFileExists(metaPath))
                {
                    targetPath = uploadedPath;
                }
                else
                {
                    // Resource not available yet, don't try to load it (will cause error)
                    // The resource will be loaded when it arrives via NetworkResourceUploadMessage
                    return;
                }
            }

            // Try to set the animation, handle errors gracefully
            try
            {
                // Check if meta.json exists (basic check)
                var metaPath = (targetPath / "meta.json").ToRootedPath();
                if (!_resource.ContentFileExists(metaPath))
                {
                    _sawmill.Debug($"RSI meta.json doesn't exist yet: {metaPath}, waiting for network load");
                    return;
                }

                var requiredState = lobbyAnimationPrototype.State;

                // Try to get the resource - this will load it if not cached
                // We don't check for individual files, just try to load and see if it works
                RSIResource? rsiResource = null;
                try
                {
                    // First try to get from cache
                    if (!_resourceCache.TryGetResource<RSIResource>(targetPath, out rsiResource))
                    {
                        _sawmill.Debug($"RSI resource not in cache, attempting to load: {targetPath}");
                        // Use useFallback: false to detect if resource actually loaded or fallback was used
                        // This prevents us from thinking the resource loaded when it actually failed
                        rsiResource = _resourceCache.GetResource<RSIResource>(targetPath, useFallback: false);
                        _sawmill.Debug($"Successfully loaded RSI resource: {targetPath}");
                    }
                    else
                    {
                        _sawmill.Debug($"RSI resource found in cache: {targetPath}");
                    }
                }
                catch (FileNotFoundException)
                {
                    // Resource file doesn't exist, wait for it to be loaded
                    // This can happen if meta.json exists but PNG files are still loading
                    _sawmill.Debug($"RSI resource not found yet: {targetPath}, waiting for network load");
                    return;
                }
                catch (Exception loadEx)
                {
                    // If loading failed, wait for resource to be fully loaded
                    // This can happen if files are partially loaded
                    _sawmill.Debug($"Failed to load lobby animation RSI: {targetPath}. Error: {loadEx.Message}. Waiting for complete resource.");
                    return;
                }

                // Verify that the resource actually loaded correctly by checking if the state exists
                if (rsiResource == null || !rsiResource.RSI.TryGetState(requiredState, out _))
                {
                    _sawmill.Debug($"RSI state '{requiredState}' not found in loaded resource: {targetPath}, waiting for complete resource");
                    return;
                }

                if (rsiResource != null)
                {
                    Lobby!.LobbyAnimation.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(targetPath, lobbyAnimationPrototype.State));
                    Lobby!.LobbyAnimation.DisplayRect.TextureScale = lobbyAnimationPrototype.Scale;
                    Lobby!.LobbyAnimation.Visible = true;
                    HideLoadingAnimation();
                    _currentAnimationPath = targetPath;
                }
                else
                {
                    _sawmill.Warning($"Failed to load lobby animation RSI: {targetPath}. Resource not found in cache.");
                    ShowLoadingAnimation();
                }
            }
            catch (Exception ex)
            {
                _sawmill.Warning($"Exception while setting lobby animation {lobbyAnimation}: {ex.Message}");
            }
        }

        private void SetLobbyArt(string lobbyArt)
        {
            // Check if art background type is currently selected
            var backgroundType = _cfg.GetCVar(SunriseCCVars.LobbyBackgroundType);
            if (backgroundType == "Random" && _gameTicker.LobbyType != null)
            {
                backgroundType = _gameTicker.LobbyType;
            }

            if (!Enum.TryParse(backgroundType, out LobbyBackgroundType lobbyBackgroundType) ||
                lobbyBackgroundType != LobbyBackgroundType.Art)
            {
                // Art is not the selected background type, don't load it
                return;
            }

            if (!_protoMan.TryIndex<LobbyArtPrototype>(lobbyArt, out var lobbyArtPrototype))
                return;

            if (Lobby == null)
            {
                _sawmill.Error("Error in SetLobbyArt. Lobby is null");
                return;
            }

            // Hide old art and show loading animation immediately
            Lobby!.LobbyArt.Visible = false;
            ShowLoadingAnimation();

            // Unload previous art if CVar is enabled
            if (_cfg.GetCVar(SunriseCCVars.LobbyUnloadResources) && _currentArtPath.HasValue)
            {
                UnloadResource(_currentArtPath.Value);
            }

            var imagePath = lobbyArtPrototype.Background;

            // Check if resource is available, request if not
            var isAvailable = _netTexturesManager.EnsureResource(imagePath);

            ResPath targetPath;
            if (isAvailable)
            {
                // Resource is available, use uploaded path
                targetPath = _netTexturesManager.GetUploadedPath(imagePath);
            }
            else
            {
                // Resource is being requested, try to use uploaded path first
                var uploadedPath = _netTexturesManager.GetUploadedPath(imagePath);

                // Check if uploaded resource exists
                if (_resource.ContentFileExists(uploadedPath))
                {
                    targetPath = uploadedPath;
                }
                else
                {
                    // Resource not available yet, show loading animation
                    // The resource will be loaded when it arrives via NetworkResourceUploadMessage
                    return;
                }
            }

            // Try to set the art, handle errors gracefully
            try
            {
                if (_resourceCache.TryGetResource<TextureResource>(targetPath, out var textureResource))
                {
                    Lobby!.LobbyArt.Texture = textureResource.Texture;
                    Lobby!.LobbyArt.Visible = true;
                    HideLoadingAnimation();
                    _currentArtPath = targetPath;
                }
                else
                {
                    _sawmill.Warning($"Failed to load lobby art texture: {targetPath}");
                    // Keep loading animation visible
                }
            }
            catch (Exception ex)
            {
                _sawmill.Warning($"Exception while setting lobby art {lobbyArt}: {ex.Message}");
            }
        }

        private void SetLobbyParallax(string lobbyParallax)
        {
            // Check if parallax background type is currently selected
            var backgroundType = _cfg.GetCVar(SunriseCCVars.LobbyBackgroundType);
            if (backgroundType == "Random" && _gameTicker.LobbyType != null)
            {
                backgroundType = _gameTicker.LobbyType;
            }

            if (!Enum.TryParse(backgroundType, out LobbyBackgroundType lobbyBackgroundType) ||
                lobbyBackgroundType != LobbyBackgroundType.Parallax)
            {
                // Parallax is not the selected background type, don't load it
                return;
            }

            if (!_protoMan.TryIndex<LobbyParallaxPrototype>(lobbyParallax, out var lobbyParallaxPrototype))
                return;

            if (Lobby == null)
            {
                _sawmill.Error("Error in SetLobbyParallax. Lobby is null");
                return;
            }

            // Show loading animation for parallax (it may load network textures)
            ShowLoadingAnimation();

            // Subscribe to resource loaded events to hide loading animation when parallax textures are ready
            void OnParallaxResourceLoaded(string resourcePath)
            {
                // Check if parallax is loaded
                if (_parallaxManager.IsLoaded(lobbyParallaxPrototype.Parallax))
                {
                    _netTexturesManager.ResourceLoaded -= OnParallaxResourceLoaded;
                    HideLoadingAnimation();
                }
            }

            _netTexturesManager.ResourceLoaded += OnParallaxResourceLoaded;

            _parallaxManager.LoadParallaxByName(lobbyParallaxPrototype.Parallax).ContinueWith(task =>
            {
                // Hide loading animation when parallax loading completes
                if (Lobby != null && _parallaxManager.IsLoaded(lobbyParallaxPrototype.Parallax))
                {
                    _netTexturesManager.ResourceLoaded -= OnParallaxResourceLoaded;
                    HideLoadingAnimation();
                }
            });

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
            var animationSetting = _cfg.GetCVar(SunriseCCVars.LobbyAnimation);
            if (animationSetting == "Random")
            {
                // For Random, use the game ticker's selected animation
                if (_gameTicker.LobbyAnimation != null)
                {
                    SetLobbyAnimation(_gameTicker.LobbyAnimation);
                }
            }
            else
            {
                // For specific animation, use the setting
                SetLobbyAnimation(animationSetting);
            }
        }

        private void UpdateLobbyArt()
        {
            var artSetting = _cfg.GetCVar(SunriseCCVars.LobbyArt);
            if (artSetting == "Random")
            {
                // For Random, use the game ticker's selected art
                if (_gameTicker.LobbyArt != null)
                {
                    SetLobbyArt(_gameTicker.LobbyArt);
                }
            }
            else
            {
                // For specific art, use the setting
                SetLobbyArt(artSetting);
            }
        }

        private void UpdateLobbyParallax()
        {
            var parallaxSetting = _cfg.GetCVar(SunriseCCVars.LobbyParallax);
            if (parallaxSetting == "Random")
            {
                // For Random, use the game ticker's selected parallax
                if (_gameTicker.LobbyParallax != null)
                {
                    SetLobbyParallax(_gameTicker.LobbyParallax);
                }
            }
            else
            {
                // For specific parallax, use the setting
                SetLobbyParallax(parallaxSetting);
            }
        }

        private void OnNetworkResourceLoaded(string resourcePath)
        {
            // Only update the resource that matches the current background type
            var backgroundType = _cfg.GetCVar(SunriseCCVars.LobbyBackgroundType);
            if (backgroundType == "Random" && _gameTicker.LobbyType != null)
            {
                backgroundType = _gameTicker.LobbyType;
            }

            if (!Enum.TryParse(backgroundType, out LobbyBackgroundType lobbyBackgroundType))
            {
                lobbyBackgroundType = LobbyBackgroundType.Parallax; // Default
            }

            // Only load the resource for the currently selected background type
            switch (lobbyBackgroundType)
            {
                case LobbyBackgroundType.Animation:
                    var currentAnimation = _cfg.GetCVar(SunriseCCVars.LobbyAnimation);
                    if (currentAnimation != null)
                    {
                        if (currentAnimation == "Random")
                        {
                            // For Random, use the game ticker's selected animation
                            if (_gameTicker.LobbyAnimation != null)
                            {
                                SetLobbyAnimation(_gameTicker.LobbyAnimation);
                            }
                        }
                        else
                        {
                            // For specific animation, always try to set it
                            SetLobbyAnimation(currentAnimation);
                        }
                    }
                    break;

                case LobbyBackgroundType.Art:
                    var currentArt = _cfg.GetCVar(SunriseCCVars.LobbyArt);
                    if (currentArt != null)
                    {
                        var artToSet = currentArt == "Random" ? _gameTicker.LobbyArt : currentArt;
                        if (artToSet != null)
                        {
                            SetLobbyArt(artToSet);
                        }
                    }
                    break;

                case LobbyBackgroundType.Parallax:
                    // Parallax doesn't need network resources, it uses local resources
                    // But we can update it if needed
                    var currentParallax = _cfg.GetCVar(SunriseCCVars.LobbyParallax);
                    if (currentParallax != null)
                    {
                        var parallaxToSet = currentParallax == "Random" ? _gameTicker.LobbyParallax : currentParallax;
                        if (parallaxToSet != null)
                        {
                            SetLobbyParallax(parallaxToSet);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Shows loading animation on the currently visible background element.
        /// </summary>
        private void ShowLoadingAnimation()
        {
            if (Lobby == null)
                return;

            try
            {
                Lobby.LoadingAnimationContainer.Visible = true;
                Lobby.LoadingAnimationContainer.SetPositionLast();
            }
            catch (Exception ex)
            {
                _sawmill.Warning($"Failed to show loading animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides loading animation.
        /// </summary>
        private void HideLoadingAnimation()
        {
            if (Lobby != null)
            {
                Lobby.LoadingAnimationContainer.Visible = false;
            }
        }

        /// <summary>
        /// Checks if all RSI files are present by reading meta.json and verifying all PNG files exist.
        /// Uses simple string parsing instead of JsonDocument to avoid sandbox restrictions.
        /// </summary>
        private bool CheckRsiFilesComplete(ResPath rsiPath, ResPath metaPath)
        {
            try
            {
                // Read meta.json
                if (!_resource.TryContentFileRead(metaPath, out var metaStream))
                {
                    return false;
                }

                using (metaStream)
                {
                    // Read JSON text
                    using var reader = new StreamReader(metaStream);
                    var jsonText = reader.ReadToEnd();

                    // Simple regex to extract state names from JSON
                    // Matches "name": "statename" patterns
                    var namePattern = new Regex(@"""name""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
                    var matches = namePattern.Matches(jsonText);

                    if (matches.Count == 0)
                    {
                        // No states found, might be invalid JSON or empty states array
                        return false;
                    }

                    // Check if all PNG files for each state exist
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count < 2)
                            continue;

                        var stateName = match.Groups[1].Value;
                        if (string.IsNullOrEmpty(stateName))
                        {
                            continue;
                        }

                        // Check if PNG file exists for this state
                        var pngPath = (rsiPath / $"{stateName}.png").ToRootedPath();
                        if (!_resource.ContentFileExists(pngPath))
                        {
                            _sawmill.Debug($"RSI file missing: {pngPath}");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _sawmill.Debug($"Error checking RSI files completeness: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unloads a resource from video memory by disposing it.
        /// TODO: Full resource unloading from cache is not currently possible due to sandbox restrictions.
        /// The ResourceCache does not expose a public API to remove resources from its internal cache.
        /// Reflection cannot be used in the client sandbox environment.
        /// This method currently only disposes the resource, but it remains in the cache.
        /// When the engine provides a proper API for resource cache management, this should be updated.
        /// </summary>
        private void UnloadResource(ResPath resourcePath)
        {
            try
            {
                bool unloaded = false;

                // Try to unload RSI resource
                if (_resourceCache.TryGetResource<RSIResource>(resourcePath, out var rsiResource))
                {
                    rsiResource.Dispose();
                    unloaded = true;
                    _sawmill.Debug($"Disposed RSI resource: {resourcePath} (still in cache due to sandbox limitations)");
                }
                // Try to unload texture resource
                else if (_resourceCache.TryGetResource<TextureResource>(resourcePath, out var textureResource))
                {
                    textureResource.Dispose();
                    unloaded = true;
                    _sawmill.Debug($"Disposed texture resource: {resourcePath} (still in cache due to sandbox limitations)");
                }

                if (!unloaded)
                {
                    _sawmill.Debug($"Resource not found in cache: {resourcePath}");
                }
            }
            catch (Exception ex)
            {
                _sawmill.Warning($"Failed to unload resource {resourcePath}: {ex.Message}");
            }
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
