using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;

namespace Content.Shared._Sunrise.Radio;

public sealed class SunriseHeadsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadsetComponent, HeadsetToggleChannelMessage>(OnToggleChannel);
        SubscribeLocalEvent<HeadsetComponent, HeadsetChangeVolumeMessage>(OnChangeVolume);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnEncryptionChannelsChanged);
    }

    /// <summary>
    ///     When channels are recalculated, disable Common channel by default
    ///     unless the user has explicitly toggled it.
    /// </summary>
    private void OnEncryptionChannelsChanged(Entity<HeadsetComponent> ent, ref EncryptionChannelsChangedEvent args)
    {
        if (args.Component.Channels.Contains(SharedChatSystem.CommonChannel) &&
            ent.Comp.EnabledChannels.TryAdd(SharedChatSystem.CommonChannel.Id, false))
        {
            Dirty(ent, ent.Comp);
        }
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
