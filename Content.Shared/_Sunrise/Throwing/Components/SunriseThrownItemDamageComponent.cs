using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Throwing.Components;

/// <summary>
///     Sunrise-Edit: component for size-based throwing damage and effects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SunriseThrownItemDamageComponent : Component
{
    [DataField("damageTypes"), AutoNetworkedField]
    public DamageSpecifier DamageTypes = new();

    [DataField, AutoNetworkedField]
    public bool IgnoreResistances = false;

    [DataField, AutoNetworkedField]
    public float WeightMultiplier = 1.0f;

    [DataField, AutoNetworkedField]
    public int KnockdownWeightThreshold = 16;

    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float BounceMultiplier = -1.0f;

    [DataField, AutoNetworkedField]
    public int StructureDamageWeightThreshold = 8;

    [DataField, AutoNetworkedField]
    public float? OriginalLinearDamping;
}
