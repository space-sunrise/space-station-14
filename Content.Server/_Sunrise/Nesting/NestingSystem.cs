using Content.Shared._Sunrise.Nesting;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.Nesting;

public sealed partial class NestingSystem : SharedNestingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NestingContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
    }
    private void OnEntityRemoved(EntityUid uid,
        NestingContainerComponent component,
        EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != BaseStorageId)
            return;

        if (!TryComp<NestingMobComponent>(args.Entity, out var nestingComponent))
            return;

        nestingComponent.InContainer = false;
        Dirty(args.Entity, nestingComponent);
    }
}
