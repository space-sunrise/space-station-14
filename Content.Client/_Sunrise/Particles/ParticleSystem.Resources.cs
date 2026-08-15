using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Particles;

public sealed partial class ParticleSystem
{
    // Разрешение графических ресурсов, форм эмиссии, кривых и шума.
    private void ResolveFrames(ActiveEmitter emitter)
    {
        var protoId = emitter.Proto.ID;

        if (_frameCache.TryGetValue(protoId, out var cached))
        {
            emitter.Frames = cached.Frames;
            emitter.Delays = cached.Delays;
            return;
        }

        if (_frameResolveFailures.Contains(protoId))
            return;

        Texture[] frames = Array.Empty<Texture>();
        float[] delays = Array.Empty<float>();

        switch (emitter.Proto.Sprite)
        {
            case SpriteSpecifier.Rsi rsi:
            {
                RSI? resource;
                try
                {
                    var path = rsi.RsiPath.IsRooted
                        ? rsi.RsiPath
                        : SpriteSpecifierSerializer.TextureRoot / rsi.RsiPath;
                    resource = _resourceCache.GetResource<RSIResource>(path).RSI;
                }
                catch
                {
                    _frameResolveFailures.Add(protoId);
                    return;
                }

                if (!resource.TryGetState(rsi.RsiState, out var state))
                {
                    _frameResolveFailures.Add(protoId);
                    return;
                }

                frames = state.GetFrames(RsiDirection.South);
                delays = state.GetDelays();
                break;
            }
            case SpriteSpecifier.Texture tex:
            {
                try { frames = new[] { _spriteSystem.Frame0(tex) }; }
                catch
                {
                    _frameResolveFailures.Add(protoId);
                    return;
                }
                break;
            }
            default:
                _frameResolveFailures.Add(protoId);
                return;
        }

        _frameCache[protoId] = (frames, delays);
        emitter.Frames = frames;
        emitter.Delays = delays;
    }

    private Vector2 SampleEmissionShape(
        EmissionShapeType shape,
        float radius,
        Vector2 boxExtents,
        Vector2 lineStart,
        Vector2 lineEnd,
        float triangleLength,
        float triangleHalfWidth,
        float emitAngle)
    {
        switch (shape)
        {
            case EmissionShapeType.Point:
                return Vector2.Zero;
            case EmissionShapeType.CircleEdge:
            {
                var a = _random.NextFloat(0f, MathF.PI * 2f);
                return new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            }
            case EmissionShapeType.CircleFill:
            {
                var a = _random.NextFloat(0f, MathF.PI * 2f);
                var r = radius * MathF.Sqrt(_random.NextFloat(0f, 1f));
                return new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
            }
            case EmissionShapeType.Box:
            {
                return new Vector2(_random.NextFloat(-boxExtents.X, boxExtents.X),
                                   _random.NextFloat(-boxExtents.Y, boxExtents.Y));
            }
            case EmissionShapeType.Line:
            {
                var offset = Vector2.Lerp(lineStart, lineEnd, _random.NextFloat(0f, 1f));
                return RotateEmissionOffset(offset, emitAngle);
            }
            case EmissionShapeType.Triangle:
            {
                var distanceRatio = MathF.Sqrt(_random.NextFloat(0f, 1f));
                var baseOffset = _random.NextFloat(-MathF.Abs(triangleHalfWidth), MathF.Abs(triangleHalfWidth));
                var offset = new Vector2(
                    baseOffset * distanceRatio,
                    MathF.Max(0f, triangleLength) * distanceRatio);
                return RotateEmissionOffset(offset, emitAngle);
            }
            default:
                return Vector2.Zero;
        }
    }

    private static Vector2 RotateEmissionOffset(Vector2 offset, float emitAngle)
    {
        var cos = MathF.Cos(emitAngle);
        var sin = MathF.Sin(emitAngle);
        return new Vector2(
            offset.X * cos + offset.Y * sin,
            -offset.X * sin + offset.Y * cos);
    }

    public static float SampleCurve(List<ParticleCurveKey> curve, float t)
    {
        if (curve.Count == 0)
            return 1f;
        if (curve.Count == 1)
            return curve[0].Value;

        ParticleCurveKey? prev = null, next = null;
        foreach (var key in curve)
        {
            if (key.Time <= t)
                prev = key;
            else
            {
                next = key;
                break;
            }
        }
        if (prev == null)
            return curve[0].Value;
        if (next == null)
            return prev.Value;

        var span = next.Time - prev.Time;
        if (span <= 0f)
            return prev.Value;
        return prev.Value + (next.Value - prev.Value) * ((t - prev.Time) / span);
    }

    public static Color SampleColorCurve(List<ColorCurveKey> curve, float t)
    {
        if (curve.Count == 0)
            return Color.White;
        if (curve.Count == 1)
            return curve[0].Color;

        ColorCurveKey? prev = null, next = null;
        foreach (var key in curve)
        {
            if (key.Time <= t)
                prev = key;
            else
            {
                next = key;
                break;
            }
        }
        if (prev == null)
            return curve[0].Color;
        if (next == null)
            return prev.Color;

        var span = next.Time - prev.Time;
        if (span <= 0f)
            return prev.Color;
        return Color.InterpolateBetween(prev.Color, next.Color, (t - prev.Time) / span);
    }

    public static Vector2 SampleVector2Curve(List<Vector2CurveKey> curve, float t)
    {
        if (curve.Count == 0)
            return Vector2.Zero;
        if (curve.Count == 1)
            return curve[0].Value;

        Vector2CurveKey? prev = null, next = null;
        foreach (var key in curve)
        {
            if (key.Time <= t)
                prev = key;
            else
            {
                next = key;
                break;
            }
        }
        if (prev == null)
            return curve[0].Value;
        if (next == null)
            return prev.Value;

        var span = next.Time - prev.Time;
        if (span <= 0f)
            return prev.Value;
        return Vector2.Lerp(prev.Value, next.Value, (t - prev.Time) / span);
    }

    /// <summary>
    /// A simple 2D value noise function for particle turbulence. Not Perlin or Simplex, just a grid of random values with smooth interpolation.
    /// </summary>
    private static float ValueNoise(float x, float y)
    {
        var ix = (int)MathF.Floor(x);
        var iy = (int)MathF.Floor(y);
        var fx = x - ix;
        var fy = y - iy;

        // Smooth interpolation
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);

        var a = Hash(ix,     iy);
        var b = Hash(ix + 1, iy);
        var c = Hash(ix,     iy + 1);
        var d = Hash(ix + 1, iy + 1);

        return a + (b - a) * fx + (c - a) * fy + (d - b - c + a) * fx * fy;
    }

    private static float Hash(int x, int y)
    {
        var n = x + y * 57;
        n = (n << 13) ^ n;
        return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
    }
}
