using Robust.Shared.Audio;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;

namespace Content.Shared.Burial.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GraveComponent : Component
{
    [DataField("digDelay")]
    public float DigDelay = 5.0f;

    [DataField("digSound")]
    public SoundSpecifier DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");

    [DataField("digOutByHandModifier")]
    public float DigOutByHandModifier = 0.5f;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool DiggingComplete = false;

    // Аудио
    [ViewVariables]
    public EntityUid? Stream = null;

    [ViewVariables]
    public DoAfterId? ShovelDiggingDoAfterId = null;

    [ViewVariables]
    public DoAfterId? HandDiggingDoAfterId = null;
}
