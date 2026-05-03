using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.GameTicking.Prototypes;

[Prototype]
public sealed partial class RoundEndMusicPoolPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<RoundEndMusicPoolPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<RoundEndMusicEntry> Tracks { get; private set; } = [];
}

[DataDefinition]
public readonly partial record struct RoundEndMusicEntry
{
    /// <summary>
    ///     Supports any <see cref="SoundSpecifier"/>, including a direct path or a sound collection.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound { get; init; } = default!;

    [DataField]
    public float Weight { get; init; } = 1f;
}
