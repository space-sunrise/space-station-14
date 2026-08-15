using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Maps a group of audible emotes to one particle orchestra.
/// </summary>
[Prototype]
public sealed partial class EmoteParticleVisualsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Emotes that use this visual presentation.</summary>
    [DataField(required: true)]
    public HashSet<ProtoId<EmotePrototype>> Emotes = [];

    /// <summary>Orchestra sent after one of the configured emote sounds starts.</summary>
    [DataField(required: true)]
    public ProtoId<ParticleOrchestraPrototype> Orchestra;
}
