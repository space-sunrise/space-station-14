using Content.Shared._Sunrise.Standing.Systems;
using Robust.Shared.Audio;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Stunnable;

public sealed partial class CrawlerComponent
{
    /// <summary>
    /// Sound used by the footstep system while this entity crawls in the downed state.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedSunriseStandingStateSystem))]
    public SoundSpecifier? CrawlingSound = new SoundCollectionSpecifier("VentClaw",
        AudioParams.Default.WithVolume(-4f).WithMaxDistance(6f)); // Sunrise-Edit
}
