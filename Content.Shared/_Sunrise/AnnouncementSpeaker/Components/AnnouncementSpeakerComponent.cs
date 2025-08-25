using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.AnnouncementSpeaker.Components;

/// <summary>
/// Marks an entity as a speaker that can broadcast station-wide announcements.
/// Announcements will be played spatially from this speaker with the configured range.
/// </summary>
[RegisterComponent]
public sealed partial class AnnouncementSpeakerComponent : Component
{
    /// <summary>
    /// The range at which this speaker can be heard from.
    /// </summary>
    [DataField("range")]
    public float Range = 20f;

    /// <summary>
    /// Whether this speaker is currently enabled.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    /// Volume modifier for announcements played through this speaker.
    /// </summary>
    [DataField("volumeModifier")]
    public float VolumeModifier = 1.0f;

    /// <summary>
    /// Whether this speaker requires power to function.
    /// </summary>
    [DataField("requiresPower")]
    public bool RequiresPower = true;
}