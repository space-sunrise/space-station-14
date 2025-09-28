using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ResearchRig.Components;

/// <summary>
/// Component that manages anomaly core as a battery system.
/// Cores lose 1 charge every 5 minutes when inserted.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AnomalyCoreBatteryComponent : Component
{
    /// <summary>
    /// Slot ID for the anomaly core
    /// </summary>
    [DataField]
    public string CoreSlotId = "anomaly_core_slot";

    /// <summary>
    /// How often to drain charge from the core (in seconds)
    /// </summary>
    [DataField]
    public float DrainInterval = 300f; // 5 minutes

    /// <summary>
    /// How much charge to drain each interval
    /// </summary>
    [DataField]
    public int ChargeTodrain = 1;

    /// <summary>
    /// Time until next charge drain
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan NextDrainTime;

    /// <summary>
    /// Whether the core battery is currently active (has a core inserted)
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool IsActive = false;
}
