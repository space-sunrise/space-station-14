using Content.Server.GameTicking.Presets;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server._Sunrise.Presets;

[Prototype, PublicAPI]
public sealed partial class GamePresetPoolPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Which presets are in this pool.
    /// </summary>
    [DataField("presets", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<GamePresetPrototype>), required: true)]
    public HashSet<string> Presets = new(0);
}
