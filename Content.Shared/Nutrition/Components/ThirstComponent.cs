using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ThirstSystem))]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ThirstComponent : Component
{
    // Base stuff
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("baseDecayRate")]
    [AutoNetworkedField]
    public float BaseDecayRate = 0.1f;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float ActualDecayRate;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public ThirstThreshold CurrentThirstThreshold;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public ThirstThreshold LastThirstThreshold;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("startingThirst")]
    [AutoNetworkedField]
    public float CurrentThirst = -1f;

    /// <summary>
    /// The time when the hunger will update next.
    /// </summary>
    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// The time between each update.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    // Sunrise-Start
    [DataField]
    public DamageSpecifier? DehydrationDamage;
    // Sunrise-End

    [DataField("thresholds")]
    [AutoNetworkedField]
    public Dictionary<ThirstThreshold, float> ThirstThresholds = new()
    {
        {ThirstThreshold.OverHydrated, 600.0f},
        {ThirstThreshold.Okay, 450.0f},
        {ThirstThreshold.Thirsty, 300.0f},
        {ThirstThreshold.Parched, 150.0f},
        {ThirstThreshold.Dead, 0.0f},
    };

    [DataField]
    public ProtoId<AlertCategoryPrototype> ThirstyCategory = "Thirst";

    public static readonly Dictionary<ThirstThreshold, ProtoId<AlertPrototype>> ThirstThresholdAlertTypes = new()
    {
        {ThirstThreshold.Thirsty, "Thirsty"},
        {ThirstThreshold.Parched, "Parched"},
        {ThirstThreshold.Dead, "Parched"},
    };

    // Sunrise-Start
    /// <summary>
    /// Mangleness healing amount when thirst level is Okay or higher. Negative values indicate healing (damage recovery signal).
    /// </summary>
    [DataField]
    public float ManglenessHealingOkay = -0.015f;

    /// <summary>
    /// Mangleness healing amount when thirst level is Thirsty. Negative values indicate healing (damage recovery signal).
    /// </summary>
    [DataField]
    public float ManglenessHealingThirsty = -0.0075f;

    /// <summary>
    /// Mangleness decay rate multiplier applied to BaseDecayRate when entity has Mangleness damage and thirst level is OverHydrated.
    /// </summary>
    [DataField]
    public float ManglenessDecayMultOverhydrated = 4.0f;

    /// <summary>
    /// Mangleness decay rate multiplier applied to BaseDecayRate when entity has Mangleness damage and thirst level is Okay.
    /// </summary>
    [DataField]
    public float ManglenessDecayMultOkay = 4.0f;

    /// <summary>
    /// Mangleness decay rate multiplier applied to BaseDecayRate when entity has Mangleness damage and thirst level is Thirsty.
    /// </summary>
    [DataField]
    public float ManglenessDecayMultThirsty = 2.0f;

    /// <summary>
    /// Tracks whether the entity previously had active Mangleness damage to detect transitions for threshold effects.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public bool HadMangleness;
    // Sunrise-End
}

[Flags]
public enum ThirstThreshold : byte
{
    // Hydrohomies
    Dead = 0,
    Parched = 1 << 0,
    Thirsty = 1 << 1,
    Okay = 1 << 2,
    OverHydrated = 1 << 3,
}
