using Content.Shared.Radio.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.Radio;

public sealed class SunriseHeadsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadsetComponent, HeadsetToggleChannelMessage>(OnToggleChannel);
        SubscribeLocalEvent<HeadsetComponent, HeadsetChangeVolumeMessage>(OnChangeVolume);
    }

    private void OnToggleChannel(Entity<HeadsetComponent> ent, ref HeadsetToggleChannelMessage args)
    {
        ent.Comp.EnabledChannels[args.ChannelId] = args.Enabled;
        Dirty(ent, ent.Comp);
    }

    private void OnChangeVolume(Entity<HeadsetComponent> ent, ref HeadsetChangeVolumeMessage args)
    {
        ent.Comp.ChannelVolumes[args.ChannelId] = args.Volume;
        Dirty(ent, ent.Comp);
    }
}
