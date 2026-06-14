using Robust.Shared.Audio;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;

namespace Content.Shared.Burial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GraveComponent : Component
{
    /// <summary>
    /// How long it takes to dig this grave, without modifiers.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DigDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Sound to play when digging this grave.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");

    /// <summary>
    /// Modifier for digging out by hand if buried alive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DigOutByHandModifier = 0.2f;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool DiggingComplete = false;

    [NonSerialized]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Stream = null;

    [NonSerialized]
    [ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? ShovelDiggingDoAfterId = null;

    [NonSerialized]
    [ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? HandDiggingDoAfterId = null;
}
