using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Presets;

[Prototype, PublicAPI]
public sealed partial class GamePresetPoolPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Presets with their respective player limits.
    /// </summary>
    [DataField("presets", required: true)]
    public Dictionary<string, int[]> Presets { get; private set; } = new();
}
