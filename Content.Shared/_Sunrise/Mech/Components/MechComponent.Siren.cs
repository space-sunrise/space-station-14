using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech.Components;

public sealed partial class MechComponent
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Siren = false;

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
