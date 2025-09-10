using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.AutoInjector.Components;

/// <summary>
/// Component for auto-injectors that defines the conditions under which 
/// they should automatically inject into the wearer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoInjectorTriggerComponent : Component
{
    /// <summary>
    /// Minimum total damage threshold to trigger this auto-injector.
    /// If total damage exceeds this value, the injector will be used.
    /// </summary>
    [DataField]
    public float TotalDamageThreshold = 50.0f;

    /// <summary>
    /// Specific damage type thresholds. If any of these damage types
    /// exceed their threshold, the injector will be used.
    /// </summary>
    [DataField]
    public Dictionary<string, float> DamageTypeThresholds = new();

    /// <summary>
    /// Priority for this auto-injector. Higher priority injectors 
    /// will be used first when multiple triggers are met.
    /// </summary>
    [DataField]
    public int Priority = 0;

    /// <summary>
    /// Whether this auto-injector has been used and should be removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsUsed = false;

    /// <summary>
    /// Optional message to display when the auto-injector triggers.
    /// </summary>
    [DataField]
    public string? TriggerMessage = null;
}