using Robust.Shared.Audio;

namespace Content.Server._Sunrise.CryoTeleport;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StationCryoTeleportComponent : Component
{
    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(300); // 5 минут

    [DataField]
    public string PortalPrototype = "PortalCryo";

    [DataField]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
