using System.Linq;
using Content.Server.Body.Components;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed partial class ImplanterSystem
{
    public const string BaseStorageId = "storagebase";

    public void InitializeImplanted()
    {
        SubscribeLocalEvent<ImplantedComponent, ComponentInit>(OnImplantedInit);
        SubscribeLocalEvent<ImplantedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImplantedComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnImplantedInit(EntityUid uid, ImplantedComponent component, ComponentInit args)
    {
        component.ImplantContainer = _container.EnsureContainer<Container>(uid, ImplanterComponent.ImplantSlotId);
        component.ImplantContainer.OccludesLight = false;
    }

    private void OnShutdown(EntityUid uid, ImplantedComponent component, ComponentShutdown args)
    {
        //If the entity is deleted, get rid of the implants
        _container.CleanContainer(component.ImplantContainer);
    }

    private void OnGibbed(Entity<ImplantedComponent> ent, ref BeingGibbedEvent args)
    {
        foreach (var implant in ent.Comp.ImplantContainer.ContainedEntities)
        {
            if (!TryComp<SubdermalImplantComponent>(implant, out var subdermalImplant))
                continue;

            if (!subdermalImplant.DropContainerItemsIfGib)
                continue;

            if (!_container.TryGetContainer(implant, BaseStorageId, out var storageImplant))
                continue;

            var entCoords = Transform(ent.Owner).Coordinates;

            var containedEntites = storageImplant.ContainedEntities.ToArray();

            foreach (var entity in containedEntites)
            {
                if (Terminating(entity))
                    continue;

                _container.RemoveEntity(storageImplant.Owner, entity, force: true, destination: entCoords);
            }
        }
        //If the entity is gibbed, get rid of the implants
        _container.CleanContainer(ent.Comp.ImplantContainer);
    }
}
