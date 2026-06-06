using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech.Components;

public sealed partial class MechComponent
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Siren = false;

    [DataField]
    public string MessageEnableSiren = "mech-message-enable-siren";

    [DataField]
    public string MessageDisableSiren = "mech-message-disable-siren";

    [DataField]
    public EntProtoId MechSirenAction = "ActionMechSiren";

    [DataField, AutoNetworkedField]
    public string? SirenState;

    [DataField, AutoNetworkedField]
    public bool SirenEnabled;

    [DataField]
    public SoundSpecifier SirenSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/policesiren.ogg");

    [AutoNetworkedField]
    public EntityUid? MechSirenActionEntity;

    [ViewVariables]
    public EntityUid? SirenStream;
}
