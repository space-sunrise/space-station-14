namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /// <summary>
    /// Проверяет Sunrise-газ, подавляющий воспламенение смеси.
    /// </summary>
    private static bool IsSunriseIgnitionSuppressed(GasMixture mixture)
    {
        return mixture.GetMoles(Gas.HyperNoblium) >= Atmospherics.ReactionSuppressionThreshold;
    }
}
