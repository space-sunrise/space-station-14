using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CarpQueen;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedCarpQueenSystem), typeof(CarpQueenAccessSystem))]
[AutoGenerateComponentState]
public sealed partial class CarpQueenServantComponent : Component
{
    [DataField("queen"), AutoNetworkedField]
    public EntityUid? Queen;
}


