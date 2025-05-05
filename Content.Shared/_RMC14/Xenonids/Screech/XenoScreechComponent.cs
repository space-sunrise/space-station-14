using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Screech;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoScreechComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectScreechShort";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_queen_screech.ogg", AudioParams.Default.WithVolume(-7).WithPlayOffset(1.4f));
}
