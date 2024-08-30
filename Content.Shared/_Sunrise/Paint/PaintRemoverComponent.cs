using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Paint;

/// <summary>
///  Removes paint from an entity that was painted with spray paint.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(PaintRemoverSystem))]
public sealed partial class PaintRemoverComponent : Component
{
    /// <summary>
    /// Sound when target is cleaned.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");

    /// <summary>
    /// DoAfter wait time.
    /// </summary>
    [DataField]
    public float CleanDelay = 2f;
}
