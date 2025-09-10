using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.AutoInjector.Components;

/// <summary>
/// Component for clothing (mainly hardsuits) that provides slots for auto-injectors
/// that automatically inject when damage thresholds are met.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoInjectorSlotComponent : Component
{
    /// <summary>
    /// Maximum number of auto-injectors this slot can hold.
    /// </summary>
    [DataField]
    public int MaxSlots = 2;

    /// <summary>
    /// List of entities currently stored as auto-injectors.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> StoredInjectors = new();

    /// <summary>
    /// Cooldown time (in seconds) between automatic injections to prevent spam.
    /// </summary>
    [DataField]
    public float InjectionCooldown = 5.0f;

    /// <summary>
    /// Timestamp of the last automatic injection.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastInjectionTime = TimeSpan.Zero;
}