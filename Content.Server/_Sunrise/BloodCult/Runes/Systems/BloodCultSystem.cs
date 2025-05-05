using Content.Server._Sunrise.BloodCult.GameRule;
using Content.Server._Sunrise.BloodCult.Objectives.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Cuffs;
using Content.Server.DoAfter;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server.Flash;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Sunrise.BloodCult.Systems;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Roles;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.BloodCult.Runes.Systems
{
    public sealed partial class BloodCultSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly BloodCultRuleSystem _bloodCultRuleSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly CuffableSystem _cuffable = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly EmpSystem _empSystem = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly FlammableSystem _flammableSystem = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly GunSystem _gunSystem = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
        [Dependency] private readonly TileSystem _tileSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly CultistWordGeneratorManager _wordGenerator = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;
        [Dependency] private readonly PullingSystem _pulling = default!;
        [Dependency] private readonly KillCultistTargetsConditionSystem _cultistTargetsConditionSystem = default!;

        [ValidatePrototypeId<StackPrototype>]
        private static string SteelStackPrototypeId = "Steel";

        [ValidatePrototypeId<StackPrototype>]
        private static string PlasteelStackPrototypeId = "Plasteel";

        [ValidatePrototypeId<ContentTileDefinition>]
        private static string CultTilePrototypeId = "CultFloor";

        private static EntProtoId AirlockGlassCultPrototypeId = "AirlockGlassCult";
        private static EntProtoId ConstructShellPrototypeId = "ConstructShell";
        private static EntProtoId ApocalypseRunePrototypeId = "ApocalypseRune";
        private static EntProtoId RunicMetalPrototypeId = "CultRunicMetal";
        private static EntProtoId CultBarrierPrototypeId = "CultBarrier";
        private static EntProtoId CultBloodSpeelPrototypeId = "CultBloodSpell";
        private static EntProtoId TeleportInEffect = "CultTeleportInEffect";
        private static EntProtoId TeleportOutEffect = "CultTeleportOutEffect";
        private static EntProtoId HelmetPrototypeId = "ClothingHeadHelmetCult";
        private static EntProtoId ArmorPrototypeId = "ClothingOuterArmorCult";
        private static EntProtoId ShoesPrototypeId = "ClothingShoesCult";
        private static EntProtoId BolaPrototypeId = "CultBola";
        private static EntProtoId CuffsPrototypeId = "CultistCuffs";
        private static EntProtoId TeleportActionPrototypeId = "ActionCultTeleport";
        private static EntProtoId TwistedConstructionActionPrototypeId = "ActionCultTwistedConstruction";
        private static EntProtoId CultTileEffectPrototypeId = "CultTileSpawnEffect";
        public static EntProtoId ReaperConstructPrototypeId = "ReaperConstruct";
        public static EntProtoId AirlockConvertEffect = "CultAirlockGlow";

        private readonly SoundPathSpecifier _teleportInSound = new("/Audio/_Sunrise/BloodCult/veilin.ogg");
        private readonly SoundPathSpecifier _teleportOutSound = new("/Audio/_Sunrise/BloodCult/veilout.ogg");
        private readonly SoundPathSpecifier _apocRuneEndDrawing = new("/Audio/_Sunrise/BloodCult/finisheddraw.ogg");
        private readonly SoundPathSpecifier _apocRuneStartDrawing = new("/Audio/_Sunrise/BloodCult/startdraw.ogg");
        private readonly SoundPathSpecifier _narsie40Sec = new("/Audio/_Sunrise/BloodCult/40sec.ogg");
        private readonly SoundPathSpecifier _magic = new("/Audio/_Sunrise/BloodCult/magic.ogg");

        private bool _doAfterAlreadyStarted;

        private EntityUid? _playingStream;

        private float _timeToDraw;

        public override void Initialize()
        {
            base.Initialize();

            InitializeBuffSystem();
            InitializeSoulShard();
            InitializeConstructs();
            InitializeBarrierSystem();
            InitializeConstructsAbilities();
            InitializeActions();
            InitializeRunes();
        }
    }
}
