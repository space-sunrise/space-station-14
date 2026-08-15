using System.Numerics;
using Content.Shared._Sunrise.Particles;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Immutable lookup tables compiled once from prototype curves for use in per-particle hot paths.
/// </summary>
public sealed class CompiledParticleEffect
{
    private const int SampleCount = 64;

    public readonly float[]? AlphaOverLifetime;
    public readonly Color[]? ColorOverLifetime;
    public readonly float[]? EmissionOverTime;
    public readonly Vector2[]? ForceOverLifetime;
    public readonly float[]? SizeOverLifetime;
    public readonly float[]? SpeedOverLifetime;
    public readonly Vector2[]? VelocityOverLifetime;

    public CompiledParticleEffect(ParticleEffectPrototype prototype)
    {
        AlphaOverLifetime = Compile(prototype.AlphaOverLifetime);
        ColorOverLifetime = Compile(prototype.ColorOverLifetime);
        EmissionOverTime = Compile(prototype.EmissionOverTime);
        ForceOverLifetime = Compile(prototype.ForceOverLifetime);
        SizeOverLifetime = Compile(prototype.SizeOverLifetime);
        SpeedOverLifetime = Compile(prototype.SpeedOverLifetime);
        VelocityOverLifetime = Compile(prototype.VelocityOverLifetime);
    }

    public static float Sample(float[] samples, float time)
    {
        var scaled = Math.Clamp(time, 0f, 1f) * (samples.Length - 1);
        var lower = (int) scaled;
        var upper = Math.Min(lower + 1, samples.Length - 1);
        return samples[lower] + (samples[upper] - samples[lower]) * (scaled - lower);
    }

    public static Vector2 Sample(Vector2[] samples, float time)
    {
        var scaled = Math.Clamp(time, 0f, 1f) * (samples.Length - 1);
        var lower = (int) scaled;
        var upper = Math.Min(lower + 1, samples.Length - 1);
        return Vector2.Lerp(samples[lower], samples[upper], scaled - lower);
    }

    public static Color Sample(Color[] samples, float time)
    {
        var scaled = Math.Clamp(time, 0f, 1f) * (samples.Length - 1);
        var lower = (int) scaled;
        var upper = Math.Min(lower + 1, samples.Length - 1);
        return Color.InterpolateBetween(samples[lower], samples[upper], scaled - lower);
    }

    private static float[]? Compile(List<ParticleCurveKey> curve)
    {
        if (curve.Count == 0)
            return null;

        var samples = new float[SampleCount];
        for (var index = 0; index < samples.Length; index++)
            samples[index] = ParticleSystem.SampleCurve(curve, index / (float) (samples.Length - 1));
        return samples;
    }

    private static Vector2[]? Compile(List<Vector2CurveKey> curve)
    {
        if (curve.Count == 0)
            return null;

        var samples = new Vector2[SampleCount];
        for (var index = 0; index < samples.Length; index++)
            samples[index] = ParticleSystem.SampleVector2Curve(curve, index / (float) (samples.Length - 1));
        return samples;
    }

    private static Color[]? Compile(List<ColorCurveKey> curve)
    {
        if (curve.Count == 0)
            return null;

        var samples = new Color[SampleCount];
        for (var index = 0; index < samples.Length; index++)
            samples[index] = ParticleSystem.SampleColorCurve(curve, index / (float) (samples.Length - 1));
        return samples;
    }
}
