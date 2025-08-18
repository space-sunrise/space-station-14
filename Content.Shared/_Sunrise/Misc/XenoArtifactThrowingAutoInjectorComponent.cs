using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.Misc;

[RegisterComponent]
public sealed partial class XenoArtifactThrowingAutoInjectorComponent : Component
{
    [DataField("hypospraySound")] public SoundSpecifier HypospraySound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class UsedXenoArtifactThrowingAutoInjectorComponent : Component
{
    [DataField("spriteStateFull")] public string SpriteStateFull = "open";
    [DataField("spriteStateEmpty")] public string SpriteStateEmpty = "closed";
    [DataField("spriteLayerName")] public string SpriteLayerName = "base";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoArtifactThrowingAutoInjectorMarkComponent : Component
{
}
