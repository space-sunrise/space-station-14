namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Keeps particle tints readable when they are sourced from very dark lights.
/// </summary>
internal static class ParticleColorHelper
{
    private const float MinimumPeakChannel = 0.55f;

    internal static Color EnsureVisible(Color color)
    {
        color = color.WithAlpha(1f);
        var peak = MathF.Max(color.R, MathF.Max(color.G, color.B));
        if (peak >= MinimumPeakChannel)
            return color;

        if (peak <= float.Epsilon)
            return Color.White;

        var scale = MinimumPeakChannel / peak;
        return new Color(
            MathF.Min(1f, color.R * scale),
            MathF.Min(1f, color.G * scale),
            MathF.Min(1f, color.B * scale),
            1f);
    }
}
