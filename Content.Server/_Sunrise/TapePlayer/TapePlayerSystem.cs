using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Sunrise.TapePlayer;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.TapePlayer;

public sealed class TapePlayerSystem : SharedTapePlayerSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TapePlayerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TapePlayerComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TapePlayerComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<TapePlayerComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        SubscribeLocalEvent<TapePlayerComponent, TapePlayerPlayingMessage>(OnTapePlayerPlay);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerPauseMessage>(OnTapePlayerPause);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerStopMessage>(OnTapePlayerStop);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerSetTimeMessage>(OnTapePlayerSetTime);

        SubscribeLocalEvent<TapePlayerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnItemInserted(EntityUid uid, TapePlayerComponent component, EntInsertedIntoContainerMessage args)
    {
        component.InsertedTape = args.Entity;
        Dirty(uid, component);
    }

    private void OnItemRemoved(EntityUid uid, TapePlayerComponent component, EntRemovedFromContainerMessage args)
    {
        _audioSystem.Stop(component.AudioStream);
        component.AudioStream = null;
        component.InsertedTape = null;
        Dirty(uid, component);
    }

    private void OnComponentInit(EntityUid uid, TapePlayerComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, TapePlayerComponent.TapeSlotId, component.TapeSlot);
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnTapePlayerPlay(EntityUid uid, TapePlayerComponent component, ref TapePlayerPlayingMessage args)
    {
        _audioSystem.PlayPvs(component.ButtonSound, uid);
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (!TryComp<MusicTapeComponent>(component.TapeSlot.Item, out var musicTapeComponent))
            {
                return;
            }

            var audioParams = AudioParams.Default
                .WithVolume(component.Volume)
                .WithMaxDistance(component.MaxDistance)
                .WithRolloffFactor(component.RolloffFactor)
                .WithLoop(true);
            component.AudioStream = Audio.PlayPvs(musicTapeComponent.Sound, uid, audioParams)?.Entity;
            Dirty(uid, component);
        }
    }

    private void OnTapePlayerPause(Entity<TapePlayerComponent> ent, ref TapePlayerPauseMessage args)
    {
        _audioSystem.PlayPvs(ent.Comp.ButtonSound, ent.Owner);
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnTapePlayerSetTime(EntityUid uid, TapePlayerComponent component, TapePlayerSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
        }
    }

    private void OnPowerChanged(Entity<TapePlayerComponent> entity, ref PowerChangedEvent args)
    {
        if (!entity.Comp.NeedPower)
            return;

        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnTapePlayerStop(Entity<TapePlayerComponent> ent, ref TapePlayerStopMessage args)
    {
        _audioSystem.PlayPvs(ent.Comp.ButtonSound, ent.Owner);
        Stop(ent);
    }

    private void Stop(Entity<TapePlayerComponent> entity)
    {
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        Dirty(entity);
    }

    private void OnComponentShutdown(EntityUid uid, TapePlayerComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void TryUpdateVisualState(EntityUid uid, TapePlayerComponent? jukeboxComponent = null)
    {
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = TapePlayerVisualState.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = TapePlayerVisualState.Off;
        }

        _appearanceSystem.SetData(uid, TapePlayerVisuals.VisualState, finalState);
    }
}
