using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.PlanetPrison
{
    public sealed class PlanetPrisonSystem : EntitySystem
    {
        private const string AntagRole = "PlanetPrisoner";
        private const string EscapeObjective = "PlanetPrisonerEscapeObjective";
        private const string GameRule = "PlanetPrison";

        private const float EscapeDistance = 150f;

        public TimeSpan NextTick = TimeSpan.Zero;
        public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IBanManager _banManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlanetPrisonerComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<PlanetPrisonRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
            SubscribeLocalEvent<PlanetPrisonRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
        }

        private void OnObjectivesTextPrepend(EntityUid uid, PlanetPrisonRuleComponent comp, ref ObjectivesTextPrependEvent args)
        {
            var planetPrisonRule = GetPlanetPrisonRule();
            args.Text += Loc.GetString("planet-prison-round-end-result", ("count", planetPrisonRule.EscapedPrisoners.Count));
        }

        private void OnObjectivesTextGetInfo(EntityUid uid, PlanetPrisonRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
        {
            args.Minds = comp.PrisonersMinds;
            args.AgentName = Loc.GetString("planet-prisoner-round-end-name");
        }

        public override void Update(float frameTime)
        {
            if (NextTick > _timing.CurTime)
                return;

            NextTick += RefreshCooldown;

            var planetPrisonRule = GetPlanetPrisonRule();
            var planetPrisonStation = EntityQuery<PlanetPrisonStationComponent>().FirstOrDefault();
            if (planetPrisonStation == null || planetPrisonStation.PrisonGrid == EntityUid.Invalid)
                return;
            var xformPrison = Transform(planetPrisonStation.PrisonGrid);
            var prisonPosition = _transformSystem.GetMapCoordinates(xformPrison);

            base.Update(frameTime);
            var query = AllEntityQuery<PlanetPrisonerComponent, TransformComponent>();
            while (query.MoveNext(out var prisoner, out _, out var xform))
            {
                var prisonerPosition = _transformSystem.GetMapCoordinates(xform);
                if (!_mindSystem.TryGetMind(prisoner, out var mindId, out var mind))
                {
                    continue;
                }

                if (prisonerPosition.MapId != planetPrisonStation.MapId && mind.UserId != null)
                {
                    // А вот нехуй играть не по правилам
                    _banManager.CreateServerBan(mind.UserId.Value,
                        null,
                        null,
                        null,
                        null,
                        3600,
                        NoteSeverity.Medium,
                        "Автоматический бан. Покинул планету тюрьмы за заключенного.");
                    QueueDel(prisoner);
                    continue;
                }

                var distance = (prisonerPosition.Position - prisonPosition.Position).Length();

                if (!(distance > EscapeDistance))
                    continue;

                if (planetPrisonRule.EscapedPrisoners.Contains(mindId))
                    continue;

                // Не ну а чо
                QueueDel(prisoner);
                planetPrisonRule.EscapedPrisoners.Add(mindId);
            }
        }

        public PlanetPrisonRuleComponent GetPlanetPrisonRule()
        {
            var planetPrisonRule = EntityQuery<PlanetPrisonRuleComponent>().FirstOrDefault();
            if (planetPrisonRule != null)
                return planetPrisonRule;

            _gameTicker.StartGameRule(GameRule, out var ruleEntity);
            planetPrisonRule = Comp<PlanetPrisonRuleComponent>(ruleEntity);

            return planetPrisonRule;
        }

        public bool PrisonerEscaped(EntityUid mind)
        {
            var planetPrisonRule = GetPlanetPrisonRule();
            return planetPrisonRule.EscapedPrisoners.Contains(mind);
        }

        private void OnMindAdded(EntityUid uid, PlanetPrisonerComponent component, MindAddedMessage args)
        {
            var planetPrisonRule = GetPlanetPrisonRule();

            if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            {
                return;
            }

            if (component.FirstMindAdded)
                return;

            component.FirstMindAdded = true;


            if (_roleSystem.MindHasRole<PlanetPrisonerRoleComponent>(mindId))
            {
                _roleSystem.MindTryRemoveRole<PlanetPrisonerRoleComponent>(mindId);
            }

            _roleSystem.MindAddRole(mindId, new PlanetPrisonerRoleComponent
            {
                PrototypeId = AntagRole,
            });

            if (_mindSystem.TryGetSession(mind, out var session))
            {
                _chatManager.DispatchServerMessage(session, Loc.GetString("planet-prisoner-role-greeting"));
            }

            _mindSystem.TryAddObjective(mindId, mind, EscapeObjective);

            planetPrisonRule.PrisonersMinds.Add((mindId, Name(uid)));
        }
    }
}
