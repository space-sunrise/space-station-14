using Content.Server.Implants;
using Robust.Shared.Audio;

namespace Content.Server.Implants.Components;

/// <summary>
/// Randomly teleports entity when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class ScramImplantComponent : Component
{
    /// <summary>
    /// Up to how far to teleport the user
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRange = 100f; // Sunrise-Edit

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinTeleportDistance = 25f; // Sunrise-Edit

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
