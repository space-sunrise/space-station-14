using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.NameGenerators;

/// <summary>
/// This handles...
/// </summary>
public sealed class ShuttleNameGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShuttleNameGeneratorComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, ShuttleNameGeneratorComponent component, ComponentInit args)
    {
        if (!TryComp<MetaDataComponent>(uid, out var metaDataComponent))
            return;

        var prefix = _random.Pick(_prototypeManager.Index(component.NameFragments).Values);
        _meta.SetEntityName(
            uid,
            (component.EnableFragments ? component.FactionIdentificator + prefix + " - " : component.FactionIdentificator + (component.HasIdentificator ? "" : " ")) +
            metaDataComponent.EntityName.Trim() +
            " " +
            Math.Ceiling(_random.NextFloat(1f, 10f) * 100),
            metaDataComponent,
            false);
    }
}
