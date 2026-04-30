using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Whether held item movement speed modifiers apply while the holder is downed.
    /// </summary>
    public static readonly CVarDef<bool> MovementHeldItemSpeedModifiersWhenDowned =
        CVarDef.Create("movement.held_item_speed_modifiers_when_downed", false, CVar.SERVER | CVar.REPLICATED);
}
