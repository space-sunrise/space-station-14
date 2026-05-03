using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared._Sunrise.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared._Sunrise.TTS;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        // Sunrise-Start
        SubscribeLocalEvent<HeadsetComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<HeadsetComponent, ToggleHeadsetActionEvent>(OnToggleAction);
        // Sunrise-End
        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);
    }

    // Sunrise-Start
    private void OnToggleAction(EntityUid uid, HeadsetComponent component, ToggleHeadsetActionEvent args)
    {
        if (args.Handled || !component.Enabled)
            return;

        if (TryComp<ActorComponent>(args.Performer, out var actor))
        {
            _ui.TryToggleUi(uid, HeadsetUiKey.Key, actor.PlayerSession);
            args.Handled = true;
        }
    }

    private void OnActivate(EntityUid uid, HeadsetComponent component, ActivateInWorldEvent args)
    {
        if (TryComp<ActorComponent>(args.User, out var actor))
        {
            _ui.OpenUi(uid, HeadsetUiKey.Key, actor.PlayerSession);
            args.Handled = true;
        }
    }
    // Sunrise-End

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            // Sunrise-Start
            if (TryComp<HeadsetComponent>(component.Headset, out var headset) && !_powerCell.TryUseCharge(component.Headset, headset.SendChargeCost, uid))
                return;
            // Sunrise-End

            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            UpdateRadioChannels(uid, component);
            _actions.AddAction(args.Equipee, ref component.ToggleActionEntity, component.ToggleAction, uid); // Sunrise-Add
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
        _actions.RemoveAction(args.Equipee, component.ToggleActionEntity); // Sunrise-Add
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
            {
                // Sunrise-Start
                var parent = Transform(uid).ParentUid;
                RemCompDeferred<WearingHeadsetComponent>(parent);
                _actions.RemoveAction(parent, component.ToggleActionEntity);
                // Sunrise-End
            }
        }
        else if (component.IsEquipped)
        {
            // Sunrise-Start
            var parent = Transform(uid).ParentUid;
            EnsureComp<WearingHeadsetComponent>(parent).Headset = uid;
            UpdateRadioChannels(uid, component);
            _actions.AddAction(parent, ref component.ToggleActionEntity, component.ToggleAction, uid);
            // Sunrise-End
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        // Sunrise-Start
        if (component.EnabledChannels.TryGetValue(args.Channel.ID, out var enabled) && !enabled)
            return;

        if (!_powerCell.TryUseCharge(uid, component.ReceiveChargeCost))
            return;
        // Sunrise-End

        // TODO: change this when a code refactor is done
        // this is currently done this way because receiving radio messages on an entity otherwise requires that entity
        // to have an ActiveRadioComponent

        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        if (TryComp(parent, out ActorComponent? actor))
        {
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
            if (parent != args.MessageSource && HasComp<TTSComponent>(args.MessageSource))
            {
                args.Receivers.Add(parent);
            }
        }
    }
}
