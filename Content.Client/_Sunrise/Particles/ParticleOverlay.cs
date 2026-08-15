using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Particles;

/// <summary>Draws all live particles for every active emitter each frame.</summary>
public sealed class ParticleOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ParticleSystem _system;

    // Shader cache
    private readonly Dictionary<string, ShaderInstance?> _shaderCache = new();

    private readonly List<ActiveEmitter> _sortBuffer = new();
    private static readonly Comparison<ActiveEmitter> RenderLayerComparison = CompareEmitters;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ParticleOverlay(ParticleSystem system)
    {
        IoCManager.InjectDependencies(this);
        _system = system;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var mapId = args.MapId;
        var eyeAngle = (float)_eye.CurrentEye.Rotation;
        var cosR = MathF.Cos(-eyeAngle);
        var sinR = MathF.Sin(-eyeAngle);
        // Симуляция и отрисовка используют одинаковый запас за границей экрана.
        var particleBounds = args.WorldBounds.Enlarged(ParticleSystem.EmitterCullMargin);

        // Sort emitters, lowest layers render first
        _sortBuffer.Clear();
        foreach (var emitter in _system.GetEmitters())
        {
            if (emitter.MapCoords.MapId != mapId)
                continue;
            if (!particleBounds.Contains(emitter.MapCoords.Position))
                continue;
            if (emitter.Frames.Length == 0)
                continue;

            _sortBuffer.Add(emitter);
        }

        if (_sortBuffer.Count == 0)
            return;

        _sortBuffer.Sort(RenderLayerComparison);

        string? activeShader = null; // track to avoid redundant calls

        foreach (var emitter in _sortBuffer)
        {

            var proto = emitter.Proto;
            var ovr = emitter.Overrides;
            var tex = emitter.Frames[emitter.AnimFrame];
            var baseHalfSize = (ovr?.ParticleSize ?? proto.ParticleSize) * 0.5f;

            // Resolve shader override takes precedence, then prototype, then null
            var wantedShader = ovr?.Shader ?? (string.IsNullOrEmpty(proto.Shader) ? null : proto.Shader);

            if (wantedShader != activeShader)
            {
                if (wantedShader != null)
                {
                    if (!_shaderCache.TryGetValue(wantedShader, out var cached))
                    {
                        cached = _proto.TryIndex<ShaderPrototype>(wantedShader, out var shaderProto)
                            ? shaderProto.Instance()
                            : null;
                        _shaderCache[wantedShader] = cached;
                    }
                    handle.UseShader(cached);
                }
                else
                {
                    handle.UseShader(null);
                }
                activeShader = wantedShader;
            }

            var screenOrigin = emitter.MapCoords.Position;
            var spawnOffset = ParticleSystem.GetEmitterSpawnOffset(emitter);

            foreach (var particle in emitter.Particles)
            {
                var t = particle.AgeRatio;

                // Color: use ColorOverLifetime gradient if available, otherwise lerp StartColor to EndColor
                Color color;
                if (emitter.Compiled.ColorOverLifetime is { } colorSamples)
                    color = CompiledParticleEffect.Sample(colorSamples, t);
                else
                {
                    var startColor = ovr?.StartColor ?? proto.StartColor;
                    var endColor   = ovr?.EndColor   ?? proto.EndColor;
                    color = Color.InterpolateBetween(startColor, endColor, t);
                }

                // ColorOverride tint
                var tintColor = ovr?.ColorOverride ?? emitter.ColorOverride;
                if (tintColor is { } tint)
                    color = new Color(color.R * tint.R, color.G * tint.G, color.B * tint.B, color.A * tint.A);

                // AlphaOverLifetime: multiplied on top of color alpha
                if (emitter.Compiled.AlphaOverLifetime is { } alphaSamples)
                {
                    var alpha = CompiledParticleEffect.Sample(alphaSamples, t);
                    color = color.WithAlpha(color.A * alpha);
                }

                // Size: base * intensity * SizeMultiplier * SizeOverLifetime curve
                var halfSize = baseHalfSize * particle.SpawnIntensity * particle.SizeMultiplier;
                if (emitter.Compiled.SizeOverLifetime is { } sizeSamples)
                    halfSize *= CompiledParticleEffect.Sample(sizeSamples, t);
                var particleScale = Vector2.Abs(ovr?.ParticleScale ?? proto.ParticleScale);
                var halfWidth = halfSize * particleScale.X;
                var halfHeight = halfSize * particleScale.Y;

                // Convert screen-space LocalOffset to world offset
                var local = particle.LocalOffset;
                var worldOffset = new Vector2(local.X * cosR - local.Y * sinR,
                                              local.X * sinR + local.Y * cosR);

                var origin = proto.WorldSpace ? particle.SpawnOrigin : screenOrigin + spawnOffset;
                var worldPos = origin + worldOffset;

                // StretchFactor: elongate along velocity direction proportional to speed.
                // Rotation is derived from the velocity unit vector +precomputed eye cos/sin
                var stretchFactor = ovr?.StretchFactor ?? proto.StretchFactor;
                if (stretchFactor > 0f)
                {
                    var velLenSq = particle.Velocity.LengthSquared();
                    if (velLenSq > 0.001f * 0.001f)
                    {
                        var velLen = MathF.Sqrt(velLenSq);
                        var stretchY = 1f + velLen * stretchFactor;
                        // Rotate velocity unit vector by -eyeAngle using precomputed cosR/sinR.
                        // ux = vel.X/velLen,  uy = vel.Y/velLen
                        // cV = cos(-eye+velAngle) = cosR*uy - sinR*ux
                        // sV = sin(-eye+velAngle) = sinR*uy + cosR*ux
                        var invLen = 1f / velLen;
                        var ux = particle.Velocity.X * invLen;
                        var uy = particle.Velocity.Y * invLen;
                        var cV = cosR * uy - sinR * ux;
                        var sV = sinR * uy + cosR * ux;
                        handle.SetTransform(new Matrix3x2(cV, sV, -sV, cV, worldPos.X, worldPos.Y));
                        handle.DrawTextureRect(tex,
                            new Box2(-halfWidth, -halfHeight * stretchY, halfWidth, halfHeight * stretchY),
                            color);
                        continue;
                    }
                }

                // AlignToVelocity: rotate sprite to face its velocity direction.
                if (proto.AlignToVelocity)
                {
                    var velLenSq = particle.Velocity.LengthSquared();
                    if (velLenSq > 0.001f * 0.001f)
                    {
                        var invLen = 1f / MathF.Sqrt(velLenSq);
                        var ux = particle.Velocity.X * invLen;
                        var uy = particle.Velocity.Y * invLen;
                        var cos = cosR * uy - sinR * ux;
                        var sin = sinR * uy + cosR * ux;
                        handle.SetTransform(new Matrix3x2(cos, sin, -sin, cos, worldPos.X, worldPos.Y));
                        handle.DrawTextureRect(tex, new Box2(-halfWidth, -halfHeight, halfWidth, halfHeight), color);
                        continue;
                    }
                }

                // Draw with rotation applied. Rotation is in radians, positive is clockwise, and 0 means "facing up" (aligned with SCREEN/eye/whatever Y axis).
                var totalRotation = -eyeAngle + particle.Rotation;
                var cosP = MathF.Cos(totalRotation);
                var sinP = MathF.Sin(totalRotation);
                handle.SetTransform(new Matrix3x2(cosP, sinP, -sinP, cosP, worldPos.X, worldPos.Y));
                handle.DrawTextureRect(tex, new Box2(-halfWidth, -halfHeight, halfWidth, halfHeight), color);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }

    private static int CompareEmitters(ActiveEmitter left, ActiveEmitter right)
    {
        var layerComparison = (left.Overrides?.RenderLayer ?? left.Proto.RenderLayer)
            .CompareTo(right.Overrides?.RenderLayer ?? right.Proto.RenderLayer);
        if (layerComparison != 0)
            return layerComparison;

        var leftShader = left.Overrides?.Shader ?? left.Proto.Shader;
        var rightShader = right.Overrides?.Shader ?? right.Proto.Shader;
        var shaderComparison = string.CompareOrdinal(leftShader, rightShader);
        return shaderComparison != 0
            ? shaderComparison
            : string.CompareOrdinal(left.Proto.ID, right.Proto.ID);
    }
}
