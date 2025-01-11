using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.CryoTeleport;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StationCryoTeleportComponent : Component
{
    [DataField]
    public string PortalPrototype = "PortalCryo";

    [DataField]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
