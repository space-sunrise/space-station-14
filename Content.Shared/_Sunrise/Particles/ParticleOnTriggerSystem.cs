using Content.Shared.Trigger;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Converts matching gameplay triggers into prediction-aware particle visual requests.
/// </summary>
public sealed class ParticleOnTriggerSystem : XOnTriggerSystem<ParticleOnTriggerComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<ProtoId<ParticleOrchestraPrototype>, string?> _validationCache = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    protected override void OnTrigger(
        Entity<ParticleOnTriggerComponent> ent,
        EntityUid target,
        ref TriggerEvent args)
    {
        if (!TryComp<TransformComponent>(target, out var transform))
            return;

        var coordinates = _transform.GetMapCoordinates((target, transform));
        var directionTarget = target == ent.Owner
            ? args.User
            : ent.Owner;
        var predictedBy = args.Predicted
            ? args.User
            : null;
        var handled = false;

        foreach (var specifier in ent.Comp.Orchestras)
        {
            if (!TryValidateOneShot(specifier.Orchestra))
                continue;

            var particleEvent = new ParticleVisualRequestEvent(
                specifier.Orchestra,
                target,
                directionTarget,
                predictedBy,
                ColorOverride: specifier.ColorOverride,
                Intensity: specifier.Intensity,
                Coordinates: coordinates,
                ColorSource: specifier.ColorSource,
                FallbackColor: specifier.FallbackColor,
                SpawnOffset: specifier.SpawnOffset);
            RaiseLocalEvent(ref particleEvent);
            handled = true;
        }

        args.Handled |= handled;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ParticleOrchestraPrototype>() ||
            args.WasModified<ParticleEffectPrototype>())
        {
            _validationCache.Clear();
        }
    }

    private bool TryValidateOneShot(ProtoId<ParticleOrchestraPrototype> orchestra)
    {
        if (_validationCache.TryGetValue(orchestra, out var error))
            return error == null;

        var valid = ParticleOrchestraValidator.TryValidateOneShot(_prototype, orchestra, out error);
        _validationCache.Add(orchestra, error);
        if (!valid)
            Log.Error($"ParticleOnTrigger cannot start orchestra '{orchestra}': {error}");

        return valid;
    }
}
