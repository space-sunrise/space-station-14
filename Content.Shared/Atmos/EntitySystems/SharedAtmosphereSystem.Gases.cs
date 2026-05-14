using System;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Robust.Shared.Maths;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    protected float[] GasFuelMask       = new float[Atmospherics.AdjustedNumberOfGases];
    protected float[] GasOxidizerMask   = new float[Atmospherics.AdjustedNumberOfGases];
    protected float[] GasSpecificHeats  = new float[Atmospherics.AdjustedNumberOfGases];

    protected virtual void InitializeGases()
    {
        var simdLen = MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4);
        Array.Resize(ref GasFuelMask,      simdLen);
        Array.Resize(ref GasOxidizerMask,  simdLen);
        Array.Resize(ref GasSpecificHeats, simdLen);

        for (var i = 0; i < GasPrototypes.Length; i++)
        {
            var proto = GasPrototypes[i];
            GasFuelMask[i]      = proto.IsFuel      ? 1f : 0f;
            GasOxidizerMask[i]  = proto.IsOxidizer  ? 1f : 0f;
            GasSpecificHeats[i] = proto.MolarHeatCapacity;
        }
    }

    public virtual bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        => throw new NotImplementedException();

    public virtual bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        => throw new NotImplementedException();

    protected virtual float GetHeatCapacityCalculation(float[] moles, bool space)
        => throw new NotImplementedException();

    public virtual ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
        => ReactionResult.NoReaction;

    /// Sunrise start
    // ---- Test-visible aliases / shared heat-capacity API ---- //

    /// <summary>
    ///     Public read accessor for the SIMD-padded per-gas specific heats array.
    /// </summary>
    public float[] GasMolarHeatCapacities => GasSpecificHeats;

    /// <summary>
    ///     Speedup scale for heat-capacity calculations, populated from
    ///     <c>CCVars.AtmosHeatScale</c> in server/client CVars files.
    /// </summary>
    public float HeatScale { get; protected set; } = 1.0f;

    /// <summary>
    ///     Returns the heat capacity of a gas mixture.
    /// </summary>
    /// <param name="mixture">The gas mixture to evaluate.</param>
    /// <param name="applyScaling">
    ///     <c>false</c> (default) — returns the raw unscaled value.<br/>
    ///     <c>true</c> — divides by <see cref="HeatScale"/>
    ///     (the atmospheric simulation speedup factor).
    /// </param>
    public float GetHeatCapacity(GasMixture mixture, bool applyScaling = false)
    {
        var raw = GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
        return applyScaling && HeatScale > 0f ? raw / HeatScale : raw;
    }
    /// Sunrise end
}
