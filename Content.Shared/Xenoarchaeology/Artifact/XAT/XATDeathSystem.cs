using Content.Shared.Mobs;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires death of some mob near artifact.
/// </summary>
public sealed class XATDeathSystem : BaseXATSystem<XATDeathComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var targetCoords = Transform(args.Target).Coordinates;

        var query = EntityQueryEnumerator<XATDeathComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            if (!_xenoArtifactQuery.TryGetComponent(node.Attached.Value, out var artifact))
                continue;

            if (!CanTrigger((node.Attached.Value, artifact), (uid, node)))
                continue;

            var artifactCoords = Transform(node.Attached.Value).Coordinates;
            if (_transform.InRange(targetCoords, artifactCoords, comp.Range))
                Trigger((node.Attached.Value, artifact), (uid, comp, node));
        }
    }
}
