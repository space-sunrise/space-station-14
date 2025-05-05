using Content.Shared.Mobs;

namespace Content.Server._Sunrise.BecomeDustOnDeathSystem;

public sealed class BecomeDustOnDeathSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BecomeDustOnDeathComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, BecomeDustOnDeathComponent component, MobStateChangedEvent args)
    {
        var xform = Transform(uid);
        Spawn(component.SpawnOnDeathPrototype, xform.Coordinates);

        QueueDel(uid);
    }
}
