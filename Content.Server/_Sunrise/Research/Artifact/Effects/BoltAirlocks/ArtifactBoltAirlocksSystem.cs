using Content.Server._Sunrise.Helpers;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server._Sunrise.Research.Artifact.Effects.BoltAirlocks;

public sealed class ArtifactBoltAirlocksSystem : BaseXAESystem<ArtifactBoltAirlocksComponent>
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;

    protected override void OnActivated(Entity<ArtifactBoltAirlocksComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var doors = _lookup.GetEntitiesInRange<DoorBoltComponent>(coords, ent.Comp.Range, LookupFlags.Static);
        var reducedDoors = _helpers.GetPercentageOfHashSet(doors, ent.Comp.Chance);

        foreach (var door in reducedDoors)
        {
            _door.SetBoltsDown(door, true);
        }
    }
}
