using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Run the equivalent of <c>cleandevicelinks</c> before mapper save serialization.
    /// </summary>
    public static readonly CVarDef<bool> MappingAutoCleanDeviceLinks =
        CVarDef.Create("mapping.auto_clean_device_links", true, CVar.SERVERONLY);

    /// <summary>
    /// Run the equivalent of <c>fixgridatmos</c> on every grid before mapper save serialization.
    /// </summary>
    public static readonly CVarDef<bool> MappingAutoFixGridAtmos =
        CVarDef.Create("mapping.auto_fix_grid_atmos", true, CVar.SERVERONLY);

    /// <summary>
    /// Run the equivalent of <c>tilewalls</c> on every grid before mapper save serialization.
    /// </summary>
    public static readonly CVarDef<bool> MappingAutoTileWalls =
        CVarDef.Create("mapping.auto_tile_walls", true, CVar.SERVERONLY);

    /// <summary>
    /// Run the equivalent of <c>removewalleddecals</c> on every grid before mapper save serialization.
    /// </summary>
    public static readonly CVarDef<bool> MappingAutoRemoveWalledDecals =
        CVarDef.Create("mapping.auto_remove_walled_decals", true, CVar.SERVERONLY);

    /// <summary>
    /// Run the equivalent of <c>variantize</c> on every grid before mapper save serialization.
    /// </summary>
    public static readonly CVarDef<bool> MappingAutoVariantize =
        CVarDef.Create("mapping.auto_variantize", true, CVar.SERVERONLY);
}
