using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Sunrise.TraitorTarget;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.Jobs;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared.Clothing;
using Content.Shared.DetailExaminable;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.EvilTwin;

public sealed class EvilTwinSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly JobSystem _jobSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string GameRule = "EvilTwin";
    [ValidatePrototypeId<EntityPrototype>]
    private const string MindRole = "MindRoleEvilTwin";
    [ValidatePrototypeId<EntityPrototype>]
    private const string KillObjective = "KillTwinObjective";
    [ValidatePrototypeId<EntityPrototype>]
    private const string EscapeObjective = "EscapeShuttleTwinObjective";
    [ValidatePrototypeId<EntityPrototype>]
    private const string MindShield = "MindShieldImplant";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EvilTwinSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<EvilTwinComponent, MindAddedMessage>(OnMindAdded);

        SubscribeLocalEvent<EvilTwinRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
        SubscribeLocalEvent<EvilTwinRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(EntityUid uid, EvilTwinRoleComponent component, ref GetBriefingEvent args)
    {
        if (!TryComp<MindComponent>(uid, out var mind) || mind.OwnedEntity == null)
            return;
        if (HasComp<EvilTwinRoleComponent>(uid)) // don't show both briefings
            return;
        args.Append(Loc.GetString("evil-twin-role-greeting"));
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, EvilTwinRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.TwinsMinds;
        args.AgentName = Loc.GetString("evil-twin-round-end-name");
    }

    private void OnPlayerAttached(EntityUid uid, EvilTwinSpawnerComponent component, PlayerAttachedEvent args)
    {
        if (TryGetEligibleHumanoid(out var targetUid))
        {
            var spawnerCoords = Transform(uid).Coordinates;
            var spawnedTwin = TrySpawnEvilTwin(targetUid.Value, spawnerCoords);
            if (spawnedTwin != null &&
                _mindSystem.TryGetMind(args.Player, out var mindId, out var mind))
            {
                Del(uid);
                mind.CharacterName = MetaData(spawnedTwin.Value).EntityName;
                _mindSystem.TransferTo(mindId, spawnedTwin);
            }
        }
    }

    public EvilTwinRuleComponent GetEvilTwinRule()
    {
        var rule = EntityQuery<EvilTwinRuleComponent>().FirstOrDefault();
        if (rule != null)
            return rule;

        _gameTicker.StartGameRule(GameRule, out var ruleEntity);
        rule = Comp<EvilTwinRuleComponent>(ruleEntity);

        return rule;
    }

    private void OnMindAdded(EntityUid uid, EvilTwinComponent component, MindAddedMessage args)
    {
        var evilTwinRule = GetEvilTwinRule();

        if (!TryComp<EvilTwinComponent>(uid, out var evilTwin) ||
            !_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        _roleSystem.MindAddRole(mindId, MindRole);
        _mindSystem.TryAddObjective(mindId, mind, EscapeObjective);
        _mindSystem.TryAddObjective(mindId, mind, KillObjective);
        if (_mindSystem.TryGetObjectiveComp<TargetObjectiveComponent>(uid, out var obj))
        {
            if (TryComp<MindComponent>(evilTwin.TargetMindId, out var mindComponent) &&
                TryComp<AntagTargetComponent>(mindComponent.OwnedEntity, out var antagTargetCom))
            {
                antagTargetCom.KillerMind = mindId;
            }
            _target.SetTarget(uid, evilTwin.TargetMindId, obj);
        }

        evilTwinRule.TwinsMinds.Add((mindId, Name(uid)));
    }

    private bool TryGetEligibleHumanoid([NotNullWhen(true)] out EntityUid? uid)
    {
        var targets = EntityQuery<ActorComponent, AntagTargetComponent, HumanoidAppearanceComponent>().ToList();
        _random.Shuffle(targets);
        foreach (var (actor, antagTarget, _) in targets)
        {
            if (!_mindSystem.TryGetMind(actor.PlayerSession, out var mindId, out var mind)
                || mind.OwnedEntity == null
                || antagTarget.KillerMind != null)
                continue;

            if (!_jobSystem.MindTryGetJob(mindId, out _))
                continue;

            uid = mind.OwnedEntity;
            return true;
        }

        uid = null;
        return false;
    }

    private EntityUid? TrySpawnEvilTwin(EntityUid target, EntityCoordinates coords)
    {
        if (!_mindSystem.TryGetMind(target, out var mindId, out _) ||
            !TryComp<HumanoidAppearanceComponent>(target, out var humanoid) ||
            !TryComp<ActorComponent>(target, out var actor) ||
            !_prototype.TryIndex(humanoid.Species, out var species))
            return null;

        var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter;

        var twinUid = Spawn(species.Prototype, coords);
        _humanoid.LoadProfile(twinUid, pref);
        _metaDataSystem.SetEntityName(twinUid, MetaData(target).EntityName);
        if (TryComp<DetailExaminableComponent>(target, out var detail))
        {
            var detailCopy = EnsureComp<DetailExaminableComponent>(twinUid);
            detailCopy.Content = detail.Content;
        }

        if (_jobSystem.MindTryGetJob(mindId, out var jobProto) && jobProto.StartingGear != null)
        {
            var jobLoadout = LoadoutSystem.GetJobPrototype(jobProto.ID);

            if (_prototype.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
            {
                pref.Loadouts.TryGetValue(jobLoadout, out var loadout);

                if (loadout == null)
                {
                    loadout = new RoleLoadout(jobLoadout);
                    loadout.SetDefault(pref, null, _prototype, []);
                }

                _stationSpawning.EquipRoleLoadout(twinUid, loadout, roleProto);
            }

            if (_prototype.TryIndex(jobProto.StartingGear, out var gear))
            {
                _stationSpawning.EquipStartingGear(twinUid, gear);
                _stationSpawning.SetPdaAndIdCardData(twinUid, pref.Name, jobProto, _stationSystem.GetOwningStation(target));
            }

            foreach (var special in jobProto.Special)
            {
                if (special is AddComponentSpecial)
                    special.AfterEquip(twinUid);
            }
        }

        if (TryComp<DnaComponent>(target, out var dnaComponent))
        {
            var dna = EnsureComp<DnaComponent>(twinUid);
            dna.DNA = dnaComponent.DNA;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            var implantEnt = Spawn(MindShield, coords);

            if (TryComp<SubdermalImplantComponent>(implantEnt, out var implantComp))
                _subdermalImplant.ForceImplant(twinUid, implantEnt, implantComp);
        }

        EnsureComp<EvilTwinComponent>(twinUid).TargetMindId = mindId;

        return twinUid;
    }
}

