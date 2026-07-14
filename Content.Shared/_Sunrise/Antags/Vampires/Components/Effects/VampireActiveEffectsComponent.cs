using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Effects;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireRejuvenateComponent : Component
{
    public int TicksRemaining;

    public TimeSpan TickInterval = TimeSpan.FromSeconds(3.5);

    [AutoPausedField]
    public TimeSpan NextTick;

    public Dictionary<string, FixedPoint2> HealGroups = new();

    public Dictionary<string, FixedPoint2> HealTypes = new();
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireGlareDotComponent : Component
{
    public EntityUid Source;

    public float StaminaDamage;

    public int TicksRemaining;

    public TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    [AutoPausedField]
    public TimeSpan NextTick;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampirePacifyComponent : Component
{
    [AutoPausedField]
    public TimeSpan EndTime;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireInvisibilityComponent : Component
{
    [AutoPausedField]
    public TimeSpan EndTime;

    public bool HadStealthComponent;

    public bool PreviousStealthEnabled;

    public float PreviousStealthVisibility = 1f;
}

[RegisterComponent]
public sealed partial class ActiveVampireHemomancerClawsComponent : Component
{
    public EntityUid? SpawnedClaws;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireBloodBondComponent : Component
{
    public EntityUid ActionEntity;

    public float Range;

    public int BloodCostPerTick;

    public TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    [AutoPausedField]
    public TimeSpan NextTick;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireBloodBringersRiteComponent : Component
{
    public int TicksRemaining = 150;

    public int BloodCost;

    public float Range;

    public FixedPoint2 Damage;

    public FixedPoint2 HealBrute;

    public FixedPoint2 HealBurn;

    public float HealStamina;

    public TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    [AutoPausedField]
    public TimeSpan NextTick;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireEternalDarknessComponent : Component
{
    public int TicksRemaining;

    public int CurrentTick;

    public int BloodPerTick;

    public int TempDropInterval;

    public float FreezeRadius;

    public float TargetFreezeTemp;

    public float TempDropPerInterval;

    [AutoPausedField]
    public TimeSpan NextTick;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireShadowBoxingComponent : Component
{
    public EntityUid Target;

    public float Range;

    public int BrutePerTick;

    public SoundSpecifier? HitSound;

    public EntProtoId PunchEffectPrototype = "WeaponArcPunch";

    public TimeSpan TickInterval = TimeSpan.FromSeconds(0.9);

    [AutoPausedField]
    public TimeSpan NextTick;

    [AutoPausedField]
    public TimeSpan EndTime;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PendingVampireTendrilsComponent : Component
{
    public EntityCoordinates TileCoordinates;

    public EntProtoId PuddlePrototype = "PuddleBlood";

    public float TargetRange;

    public TimeSpan SlowDuration;

    public float SlowMultiplier;

    public FixedPoint2 ToxinDamage;

    [AutoPausedField]
    public TimeSpan TriggerTime;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireDemonicGraspComponent : Component
{
    public EntityCoordinates StartCoordinates;

    public EntityUid GridUid;

    public Vector2 Direction;

    public int CurrentTile;

    public int MaxTiles;

    public TimeSpan TileInterval = TimeSpan.FromMilliseconds(50);

    public TimeSpan ImmobilizeDuration;

    public bool PullTarget;

    public EntProtoId EffectPrototype = "VampireDemonicGraspEffect";

    public EntProtoId ImmobilizedEffectPrototype = "VampireImmobilizedEffect";

    [AutoPausedField]
    public TimeSpan NextTileTime;
}
