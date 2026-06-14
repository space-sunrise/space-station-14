using Robust.Shared.Audio;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;

namespace Content.Shared.Burial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GraveComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DigDelay = TimeSpan.FromSeconds(15);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DigOutByHandModifier = 0.2f;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool DiggingComplete = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Stream = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? ShovelDiggingDoAfterId = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? HandDiggingDoAfterId = null;
}
