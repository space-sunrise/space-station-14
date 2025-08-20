using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.Misc;

[RegisterComponent]
public sealed partial class XenoArtifactThrowingAutoInjectorComponent : Component
{
    [DataField]
    public SoundSpecifier HypospraySound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Был ли уже использован автоинъектор (в кого-то вонзился)
    /// </summary>
    [ViewVariables]
    public bool Used = false;
}

public enum XenoArtifactThrowingAutoInjectorVisualLayers : byte
{
    Base
}

[RegisterComponent, NetworkedComponent]
public sealed partial class UsedXenoArtifactThrowingAutoInjectorComponent : Component
{
    [DataField]
    public string SpriteStateFull = "open";
    [DataField]
    public string SpriteStateEmpty = "closed";
    [DataField]
    public XenoArtifactThrowingAutoInjectorVisualLayers SpriteLayer = XenoArtifactThrowingAutoInjectorVisualLayers.Base;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoArtifactThrowingAutoInjectorMarkComponent : Component
{
}
