using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Research.Artifact.Effects.ModifyHunger;

public sealed class ArtifactModifyHungerSystem : BaseXAESystem<ArtifactModifyHungerComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnActivated(Entity<ArtifactModifyHungerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var humans = _lookup.GetEntitiesInRange<HungerComponent>(Transform(ent).Coordinates, ent.Comp.Range);

        foreach (var uid in humans)
        {
            var modifier = _random.NextFloat(-1f, 1f);
            _hunger.ModifyHunger(uid, modifier * ent.Comp.Amount);
        }
    }
}
