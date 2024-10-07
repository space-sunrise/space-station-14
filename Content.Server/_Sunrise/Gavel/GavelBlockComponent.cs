using Robust.Shared.Audio;

namespace Content.Server._Sunrise.Gavel;

[RegisterComponent]
public sealed partial class GavelBlockComponent : Component
{
    [DataField]
    public SoundSpecifier HitSound;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);

    public TimeSpan? PrevSound;
}
