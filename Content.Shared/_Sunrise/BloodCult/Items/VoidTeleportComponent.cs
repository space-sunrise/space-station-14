using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class VoidTeleportComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Active = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    [ViewVariables(VVAccess.ReadWrite), DataField("maxRange")]
    public int MaxRange = 15;

    [ViewVariables(VVAccess.ReadWrite), DataField("minRange")]
    public int MinRange = 5;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUse = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly), DataField("teleportInEffect")]
    public string? TeleportInEffect = "CultTeleportInEffect";

    [ViewVariables(VVAccess.ReadWrite), DataField("teleportInSound")]
    public SoundSpecifier TeleportInSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/veilin.ogg");

    [ViewVariables(VVAccess.ReadOnly), DataField("teleportOutEffect")]
    public string? TeleportOutEffect = "CultTeleportOutEffect";

    [ViewVariables(VVAccess.ReadWrite), DataField("teleportOutSound")]
    public SoundSpecifier TeleportOutSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/veilout.ogg");

    public TimeSpan TimerDelay = TimeSpan.FromSeconds(0.5);

    public CancellationTokenSource Token = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("usesLeft")]
    public int UsesLeft = 4;
}

[Serializable, NetSerializable]
public enum VeilVisuals : byte
{
    Activated
}
