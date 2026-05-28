using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Guardian;

/// <summary>
/// Adds a selectable list of guardian prototypes to an existing guardian creator item.
/// </summary>
[RegisterComponent]
public sealed partial class GuardianCreatorSelectorComponent : Component
{
    /// <summary>
    /// Guardian prototypes available for this creator.
    /// </summary>
    [DataField]
    public List<GuardianCreatorSelectorOption> Options = new();

    /// <summary>
    /// Runtime-selected guardian prototype. Falls back to the first option.
    /// </summary>
    public string? SelectedPrototype;

    /// <summary>
    /// Injection targets chosen when each user opened the selector.
    /// </summary>
    public readonly Dictionary<EntityUid, EntityUid> PendingTargets = new();
}

/// <summary>
/// One guardian prototype option exposed by <see cref="GuardianCreatorSelectorComponent"/>.
/// </summary>
[DataDefinition]
public sealed partial class GuardianCreatorSelectorOption
{
    /// <summary>
    /// Prototype spawned by GuardianCreator when this option is selected.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;

    /// <summary>
    /// Locale id for the option name.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Optional locale id for the short option summary.
    /// </summary>
    [DataField]
    public string? Description;

    /// <summary>
    /// Locale id for the full details shown in the selector window.
    /// </summary>
    [DataField(required: true)]
    public string Details = string.Empty;
}
