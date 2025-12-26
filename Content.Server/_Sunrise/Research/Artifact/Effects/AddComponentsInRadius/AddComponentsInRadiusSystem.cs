using System.Linq;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server._Sunrise.Research.Artifact.Effects.AddComponentsInRadius;

public sealed class AddComponentsInRadiusSystem : BaseXAESystem<AddComponentsInRadiusComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    protected override void OnActivated(Entity<AddComponentsInRadiusComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var targets = _lookup.GetEntitiesInRange<TransformComponent>(coords, ent.Comp.Radius)
            .Where(e => _whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, e));

        foreach (var target in targets)
        {
            EntityManager.AddComponents(target, ent.Comp.Components, false);
        }
    }
}
