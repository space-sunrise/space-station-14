using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Cuffs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Alert;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Movement.Systems;
using Content.Shared.Roles;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.FleshCult;

public sealed partial class FleshCultSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly List<string> _speciesWhitelist =
    [
        "Human",
        "Reptilian",
        "Dwarf",
        "Vulpkanin",
        "Felinid",
        "Moth",
        "Swine",
        "Arachnid",
        "Demon",
        "Vox",
        "HumanoidXeno",
        "Predator",
        "Tajaran"
    ];

    public override void Initialize()
    {
        base.Initialize();

        InitializeVirus();
        InitializeAbilities();
        InitializeCultist();
        InitializeMob();
        InitializeHugger();
        InitializeHeart();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateCultist(frameTime);
        UpdateHugger(frameTime);
        UpdateVirus(frameTime);
        UpdateHeart(frameTime);
    }
}
