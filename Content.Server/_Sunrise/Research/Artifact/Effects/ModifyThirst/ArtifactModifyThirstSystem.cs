using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Research.Artifact.Effects.ModifyThirst;

public sealed class ArtifactModifyThirstSystem : BaseXAESystem<ArtifactModifyThirstComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnActivated(Entity<ArtifactModifyThirstComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var humans = _lookup.GetEntitiesInRange<ThirstComponent>(Transform(ent).Coordinates, ent.Comp.Range);

        foreach (var uid in humans)
        {
            var modifier = _random.NextFloat(-1f, 1f);
            _thirst.ModifyThirst(uid, uid, modifier * ent.Comp.Amount);
        }
    }
}
