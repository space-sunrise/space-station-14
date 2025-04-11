using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Restrict;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]

// Taken from Starlight https://github.com/ss14Starlight/space-station-14
public sealed partial class RestrictByUserTagComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Contains = [];

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> DoestContain = [];

    [DataField, AutoNetworkedField]
    public List<string> Messages = [];
}
