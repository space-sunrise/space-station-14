using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._Sunrise.NewLife.UI;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.NewLife;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Ghost;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Utility; // Sunrise-Sponsors

namespace Content.Server._Sunrise.NewLife
{
    [UsedImplicitly]
    public sealed class NewLifeSystem : SharedNewLifeSystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly StationJobsSystem _stationJobs = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly PlayTimeTrackingSystem _playTimeTrackings = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        private ISharedSponsorsManager? _sponsorsManager; // Sunrise-Sponsors

        private readonly Dictionary<ICommonSession, NewLifeEui> _openUis = new();
        public int NewLifeTimeout;
        private bool _newLifeEnable;
        private bool _newLifeSponsorOnly;

        private readonly Dictionary<NetUserId, NewLifeUserData> _newLifeRoundData = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeNetworkEvent<NewLifeOpenRequest>(OnRespawnMenuOpenRequest);
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
            _netMgr.Connecting += NetMgrOnConnecting;

            _cfg.OnValueChanged(SunriseCCVars.NewLifeTimeout, SetNewLifeTimeout, true);
            _cfg.OnValueChanged(SunriseCCVars.NewLifeEnable, SetNewLifeEnable, true);
            _cfg.OnValueChanged(SunriseCCVars.NewLifeSponsorOnly, SetNewLifeSponsorOnly, true);

            IoCManager.Instance!.TryResolveType(out _sponsorsManager); // Sunrise-Sponsors
        }

        private void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
        {
            if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
                return;

            foreach (var newLifeUserData in _newLifeRoundData)
            {
                newLifeUserData.Value.UsedCharactersForRespawn.Clear();
            }
        }


        public void SetNextAllowRespawn(NetUserId userId, TimeSpan nextRespawnTime)
        {
            if (_newLifeRoundData.TryGetValue(userId, out var data))
            {
                data.NextAllowRespawn = nextRespawnTime;
            }
        }

        public void AddUsedCharactersForRespawn(NetUserId userId, int usedCharacter)
        {
            if (_newLifeRoundData.TryGetValue(userId, out var data))
            {
                data.UsedCharactersForRespawn.Add(usedCharacter);
            }
        }

        private bool TryGetUsedCharactersForRespawn(NetUserId userId, [NotNullWhen(true)] out List<int>? usedCharactersForRespawn)
        {
            usedCharactersForRespawn = null;
            if (!_newLifeRoundData.TryGetValue(userId, out var data))
            {
                return false;
            }
            usedCharactersForRespawn = data.UsedCharactersForRespawn;
            return true;
        }

        private bool TryGetNextAllowRespawn(NetUserId userId, [NotNullWhen(true)] out TimeSpan? nextAllowRespawn)
        {
            nextAllowRespawn = null;
            if (!_newLifeRoundData.TryGetValue(userId, out var data))
            {
                return false;
            }
            nextAllowRespawn = data.NextAllowRespawn;
            return true;
        }

        private async Task NetMgrOnConnecting(NetConnectingArgs e)
        {
            if (!_newLifeEnable)
                return;
            if (_sponsorsManager != null && _sponsorsManager.IsAllowedRespawn(e.UserId) || !_newLifeSponsorOnly)
            {
                if (_newLifeRoundData.ContainsKey(e.UserId))
                    return;
                _newLifeRoundData.Add(e.UserId, new NewLifeUserData());
            }
        }

        private void SetNewLifeTimeout(int value)
        {
            NewLifeTimeout = value;
        }

        private void SetNewLifeEnable(bool value)
        {
            _newLifeEnable = value;
        }

        private void SetNewLifeSponsorOnly(bool value)
        {
            _newLifeSponsorOnly = value;
        }

        private void OnRespawnMenuOpenRequest(NewLifeOpenRequest msg, EntitySessionEventArgs args)
        {
            OpenEui(args.SenderSession);
        }

        public void OnGhostRespawnMenuRequest(ICommonSession player, int? characterId, NetEntity? stationId, string? roleProto)
        {
            var stationUid = GetEntity(stationId);
            if (stationUid == null || roleProto == null || characterId == null)
                return;
            if ((_sponsorsManager == null || !_sponsorsManager.IsAllowedRespawn(player.UserId)) && _newLifeSponsorOnly)
                return;
            if (!_stationJobs.GetAvailableJobs(stationUid.Value).Contains(roleProto))
                return;
            _prefsManager.GetPreferences(player.UserId).SetProfile(characterId.Value);
            _gameTicker.MakeJoinGame(player, stationUid.Value, roleProto, canBeAntag: false);
        }

        private void OpenEui(ICommonSession session)
        {
            if (session.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.HasComponent<GhostComponent>(attached))
                return;

            if(_openUis.ContainsKey(session))
                CloseEui(session);

            var preferencesManager = IoCManager.Resolve<IServerPreferencesManager>();
            var prefs = preferencesManager.GetPreferences(session.UserId);

            var jobsDict = new Dictionary<NetEntity, List<(JobPrototype, int?)>>();
            var stationsList = new Dictionary<NetEntity, string>();
            var stations = _stationSystem.GetStations();
            foreach (var stationUid in stations)
            {
                if (!HasComp<StationJobsComponent>(stationUid))
                    continue;

                var availableStationJobs = new List<(JobPrototype, int?)>();
                var stationJobs = _stationJobs.GetJobs(stationUid);

                foreach (var job in stationJobs)
                {
                    if (!_playTimeTrackings.IsAllowed(session, job.Key))
                        continue;
                    availableStationJobs.Add((_prototypeManager.Index(job.Key), job.Value));
                }

                if (availableStationJobs.Count == 0)
                    continue;

                availableStationJobs.Sort((x, y) =>
                {
                    var xName = x.Item1.LocalizedName;
                    var yName = y.Item1.LocalizedName;
                    return -string.Compare(xName, yName, StringComparison.CurrentCultureIgnoreCase);
                });

                jobsDict.Add(GetNetEntity(stationUid), availableStationJobs);
                stationsList.Add(GetNetEntity(stationUid), MetaData(stationUid).EntityName);
            }

            if (!TryGetNextAllowRespawn(session.UserId, out var nextAllowRespawn))
                return;

            if (!TryGetUsedCharactersForRespawn(session.UserId, out var usedCharactersForRespawn))
                return;

            var eui = _openUis[session] = new NewLifeEui(prefs.Characters, stationsList, jobsDict,
                nextAllowRespawn.Value, usedCharactersForRespawn);
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(ICommonSession session)
        {
            if (!_openUis.ContainsKey(session))
                return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }

        public void UpdateAllEui()
        {
            foreach (var eui in _openUis.Values)
            {
                eui.StateDirty();
            }
        }

        public List<NewLifeCharacterInfo> GetCharactersInfo(IReadOnlyDictionary<int, ICharacterProfile> characterProfiles)
        {
            var characters = new List<NewLifeCharacterInfo>();

            foreach (var (charKey, characterProfile) in characterProfiles)
            {
                characters.Add(new NewLifeCharacterInfo {Identifier = charKey, Name = characterProfile.Name});
            }

            return characters;
        }

        public Dictionary<NetEntity, List<NewLifeRolesInfo>> GetRolesInfo(Dictionary<NetEntity, List<(JobPrototype, int?)>> availableStations)
        {
            var stations = new Dictionary<NetEntity, List<NewLifeRolesInfo>>();

            foreach (var station in availableStations)
            {
                var roles = new List<NewLifeRolesInfo>();

                foreach (var availableJob in station.Value)
                {
                    roles.Add(new NewLifeRolesInfo {Identifier = availableJob.Item1.ID, Name = availableJob.Item1.LocalizedName, Count = availableJob.Item2});
                }

                stations.Add(station.Key, roles);
            }

            return stations;
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player))
                return;

            if (HasComp<GhostComponent>(message.Entity))
                return;

            CloseEui(message.Player);
        }
    }
}
