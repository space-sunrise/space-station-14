using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Lobby;

[Prototype]
public sealed partial class LobbyBackgroundPresetPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LobbyBackgroundPresetPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ProtoId<LobbyArtPrototype>> WhitelistArts = [];

    [DataField]
    public List<ProtoId<LobbyAnimationPrototype>> WhitelistAnimations = [];

    [DataField]
    public List<ProtoId<LobbyParallaxPrototype>> WhitelistParallaxes = [];

    [DataField]
    public bool AllArtsAllowed;

    [DataField]
    public bool AllAnimationsAllowed;

    [DataField]
    public bool AllParallaxesAllowed;
}
