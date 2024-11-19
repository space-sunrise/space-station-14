using Content.Server.EUI;
using Content.Shared._Sunrise.NewLife;
using Content.Shared.Eui;
using Content.Shared.Preferences;
using Content.Shared.Roles;

namespace Content.Server._Sunrise.NewLife.UI
{
    public sealed class NewLifeEui : BaseEui
    {
        private readonly IReadOnlyDictionary<int, ICharacterProfile> _characterProfiles;
        private readonly Dictionary<NetEntity, string> _stations;
        private readonly Dictionary<NetEntity, List<(JobPrototype, int?)>> _jobs;
        private readonly TimeSpan _nextAllowRespawn;
        private readonly List<int> _usedCharactersForRespawn;

        public NewLifeEui(IReadOnlyDictionary<int, ICharacterProfile> prefsCharacters, Dictionary<NetEntity, string> stations,
            Dictionary<NetEntity, List<(JobPrototype, int?)>> jobs, TimeSpan nextAllowRespawn, List<int> usedCharactersForRespawn)
        {
            _characterProfiles = prefsCharacters;
            _stations = stations;
            _jobs = jobs;
            _nextAllowRespawn = nextAllowRespawn;
            _usedCharactersForRespawn = usedCharactersForRespawn;
        }

        public override NewLifeEuiState GetNewState()
        {
            var newLife = EntitySystem.Get<NewLifeSystem>();

            return new(newLife.GetCharactersInfo(_characterProfiles),
                _stations,
                newLife.GetRolesInfo(_jobs),
                _nextAllowRespawn,
                _usedCharactersForRespawn);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case NewLifeRequestSpawnMessage req:
                    EntitySystem.Get<NewLifeSystem>().OnGhostRespawnMenuRequest(Player, req.CharacterId, req.StationId, req.RoleProto);
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            EntitySystem.Get<NewLifeSystem>().CloseEui(Player);
        }
    }
}
