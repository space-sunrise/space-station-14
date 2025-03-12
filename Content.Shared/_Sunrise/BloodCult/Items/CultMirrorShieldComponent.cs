using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.BloodCult.Items;

/// <summary>
///     Зеркальный щит культистов нарси
/// </summary>
[RegisterComponent]
public sealed partial class CultMirrorShieldComponent : Component
{
    /// <summary>
    ///     На сколько времени станится культист после ломания щита
    /// </summary>
    [DataField("knockdownDuration")] [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(3); // По канону 3 тика, скорее всего это около 3х секунд

    /// <summary>
    ///     Звук ломания щита
    /// </summary>
    [DataField("breakSound")] [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? BreakSound = new SoundCollectionSpecifier("GlassBreak");
}
