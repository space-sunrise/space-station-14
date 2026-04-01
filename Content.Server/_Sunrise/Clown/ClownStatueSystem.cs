using Content.Server.Actions;
using Content.Shared.Magic.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.Clown;

public sealed class ClownStatueSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClownStatueComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ClownStatueComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnStatueSpawn);
    }

    private void OnMapInit(EntityUid uid, ClownStatueComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnShutdown(EntityUid uid, ClownStatueComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnStatueSpawn(WorldSpawnSpellEvent args)
    {
        if (!HasComp<ClownStatueComponent>(args.Performer))
            return;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/bikehorn.ogg"), args.Performer);
    }
}
