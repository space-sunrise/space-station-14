using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.AutoCryostorage;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AutoCryostorageComponent : Component
{
    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(600); // 600 секунд перед перемещением в крио

    [DataField]
    public bool IsCounting = false;

    [DataField]
    public string? PortalPrototype = "PortalCryo";

    [DataField]
    public SoundSpecifier? EnterSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
