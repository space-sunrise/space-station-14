using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Particles;

/// <summary>
/// Sends compact semantic particle-orchestra events to nearby clients.
/// </summary>
public sealed class ParticleOrchestraSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleVisualRequestEvent>(OnParticleVisualRequest);
    }

    private void OnParticleVisualRequest(ref ParticleVisualRequestEvent args)
    {
        if (!TryGetCoordinates(args.Source, args.Coordinates, out var coordinates))
            return;

        var filter = Filter.Pvs(coordinates);
        if (args.PredictedBy is { } predictedBy)
            filter = filter.RemovePlayerByAttachedEntity(predictedBy);

        EntityUid? source = TerminatingOrDeleted(args.Source)
            ? null
            : args.Source;
        Send(
            args.Orchestra,
            coordinates,
            source,
            args.Target,
            args.Movement,
            args.ColorOverride,
            args.Intensity,
            args.ColorSource,
            args.FallbackColor,
            args.SpawnOffset,
            filter);
    }

    /// <summary>
    /// Sends an orchestra using the source entity as its position and visual context.
    /// </summary>
    public void Send(
        ProtoId<ParticleOrchestraPrototype> orchestra,
        EntityUid source,
        EntityUid? target = null,
        Vector2 movement = default,
        Color? colorOverride = null,
        float intensity = 1f,
        ParticleVisualColorSource colorSource = ParticleVisualColorSource.None,
        Color? fallbackColor = null,
        Vector2 spawnOffset = default,
        Filter? recipients = null)
    {
        if (TerminatingOrDeleted(source))
            return;

        Send(
            orchestra,
            _transform.GetMapCoordinates(source),
            source,
            target,
            movement,
            colorOverride,
            intensity,
            colorSource,
            fallbackColor,
            spawnOffset,
            recipients);
    }

    /// <summary>
    /// Sends an orchestra invocation while converting optional entity context to network identifiers.
    /// </summary>
    public void Send(
        ProtoId<ParticleOrchestraPrototype> orchestra,
        MapCoordinates coordinates,
        EntityUid? source = null,
        EntityUid? target = null,
        Vector2 movement = default,
        Color? colorOverride = null,
        float intensity = 1f,
        ParticleVisualColorSource colorSource = ParticleVisualColorSource.None,
        Color? fallbackColor = null,
        Vector2 spawnOffset = default,
        Filter? recipients = null)
    {
        NetEntity? sourceNetEntity = source is { } sourceEntity && Exists(sourceEntity)
            ? GetNetEntity(sourceEntity)
            : null;
        NetEntity? targetNetEntity = target is { } targetEntity && Exists(targetEntity)
            ? GetNetEntity(targetEntity)
            : null;
        var message = new ParticleVisualEvent(
            orchestra,
            coordinates,
            sourceNetEntity,
            targetNetEntity,
            movement,
            colorOverride,
            intensity,
            colorSource,
            fallbackColor,
            spawnOffset);

        RaiseNetworkEvent(message, recipients ?? Filter.Pvs(coordinates));
    }

    private bool TryGetCoordinates(
        EntityUid source,
        MapCoordinates? fallback,
        out MapCoordinates coordinates)
    {
        if (fallback is { } exactCoordinates)
        {
            coordinates = exactCoordinates;
            return true;
        }

        coordinates = default;
        if (TerminatingOrDeleted(source))
            return false;

        coordinates = _transform.GetMapCoordinates(source);
        return true;
    }
}
