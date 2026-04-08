using Content.Server.Storage.Components;
using Content.Server.Station.Systems; // Sunrise-Edit
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Storage.EntitySystems;

public sealed class CursedEntityStorageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _station = default!; // Sunrise-Edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedEntityStorageComponent, StorageAfterCloseEvent>(OnClose);
    }

    private void OnClose(EntityUid uid, CursedEntityStorageComponent component, ref StorageAfterCloseEvent args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (storage.Open || storage.Contents.ContainedEntities.Count <= 0)
            return;

        var lockers = new List<Entity<EntityStorageComponent>>();
        // Sunrise-Start
        var sourceGrid = component.SameGridOnly ? Transform(uid).GridUid : null;
        var sourceStation = component.SameGridOnly ? _station.GetOwningStation(uid) : null;
        // Sunrise-End
        var query = EntityQueryEnumerator<EntityStorageComponent>();
        while (query.MoveNext(out var storageUid, out var storageComp))
        {
            // Sunrise-Start
            if (component.SameGridOnly && Transform(storageUid).GridUid != sourceGrid)
                continue;

            if (component.SameGridOnly && _station.GetOwningStation(storageUid) != sourceStation)
                continue;
            // Sunrise-End
            lockers.Add((storageUid, storageComp));
        }

        lockers.RemoveAll(e => e.Owner == uid);

        if (lockers.Count == 0)
            return;

        var lockerEnt = _random.Pick(lockers).Owner;

        foreach (var entity in storage.Contents.ContainedEntities.ToArray())
        {
            _container.Remove(entity, storage.Contents);
            _entityStorage.AddToContents(entity, lockerEnt);
        }

        _audio.PlayPvs(component.CursedSound, uid);
    }
}
