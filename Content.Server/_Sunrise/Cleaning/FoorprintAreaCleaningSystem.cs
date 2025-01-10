using Content.Shared._Sunrise.Footprints;
using Robust.Shared.Physics.Events;

namespace Content.Server._Sunrise.Cleaning;

public sealed class FoorprintAreaCleaningSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootprintAreaCleanerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, FootprintAreaCleanerComponent component, StartCollideEvent args)
    {
        if (!TryComp<FootprintComponent>(args.OtherEntity, out _) || !component.Enabled)
            return;

        _entMan.QueueDeleteEntity(args.OtherEntity);
    }
}
