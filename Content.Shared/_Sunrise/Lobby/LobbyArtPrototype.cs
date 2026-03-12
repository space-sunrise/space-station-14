using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Lobby;

[Prototype]
public sealed partial class LobbyArtPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LobbyArtPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The sprite path to use as the background. This should ideally be 1920x1080.
    /// </summary>
    [DataField(required: true)]
    public string Background = default!;
}

