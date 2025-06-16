using System.Globalization;
using Content.Server._Sunrise.ChatSan; // sunrise-add
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server.VoiceMask;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Access.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    // Sunrise start
    private const string NoIdIconPath = "/Textures/Interface/Misc/job_icons.rsi/NoId.png";
    private const string StationAiIconPath = "/Textures/Interface/Misc/job_icons.rsi/StationAi.png";
    private const string BorgIconPath = "/Textures/_Sunrise/Interface/Misc/job_icons.rsi/Borg.png";
    // Sunrise end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        // Sunrise-TTS-Start
        if (TryComp(uid, out ActorComponent? actor))
        {
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
            if (uid != args.MessageSource && HasComp<TTSComponent>(args.MessageSource))
            {
                args.Receivers.Add(uid);
            }
        }
        // Sunrise-TTS-End
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(EntityUid messageSource, string message, ProtoId<RadioChannelPrototype> channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, escapeMarkup: escapeMarkup);
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(EntityUid messageSource, string message, RadioChannelPrototype channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        // Sunrise-Start
        var sanEvent = new ChatSanRequestEvent(message);
        RaiseLocalEvent(ref sanEvent);
        if (sanEvent.Cancelled)
            return;
        message = sanEvent.Message;
        // Sunrise-End
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        // Sunrise-Start
        var tag = Loc.GetString("radio-icon-tag",
            ("path", GetIdSprite(messageSource)),
            ("scale", "3"),
            ("text", GetIdCardName(messageSource)),
            ("color", GetIdCardColor(messageSource))
        );

        var formattedName = $"{tag} {name}";
        // Sunrise-End

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.TryIndex(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        // Sunrise-Start
        if (GetIdCardIsBold(messageSource))
        {
            content = $"[bold]{content}[/bold]";
        }
        // Sunrise-End

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
            ("color", channel.Color),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("channel", $"\\[{channel.LocalizedName}\\]"), // Sunrise-Edit
            ("name", formattedName),
            ("message", content));

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            NetEntity.Invalid,
            null);
        var chatMsg = new MsgChatMessage { Message = chat };
        var ev = new RadioReceiveEvent(message, messageSource, channel, radioSource, chatMsg, []);

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
        }

        RaiseLocalEvent(new RadioSpokeEvent(messageSource, FormattedMessage.RemoveMarkupPermissive(message), ev.Receivers.ToArray())); // Sunrise-TTS

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(chat);
        _messages.Remove(message);
    }

    // Sunrise-Start
    private IdCardComponent? GetIdCard(EntityUid senderUid)
    {
        if (!_inventorySystem.TryGetSlotEntity(senderUid, "id", out var idUid))
            return null;

        if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) && pda.ContainedId is not null)
        {
            if (TryComp<IdCardComponent>(pda.ContainedId, out var idComp))
                return idComp;
        }
        else if (EntityManager.TryGetComponent(idUid, out IdCardComponent? id))
        {
            return id;
        }

        return null;
    }

    private string GetIdCardName(EntityUid senderUid)
    {
        var idCardTitle = Loc.GetString("chat-radio-no-id");
        idCardTitle = GetIdCard(senderUid)?.LocalizedJobTitle ?? idCardTitle;

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        idCardTitle = textInfo.ToTitleCase(idCardTitle);

        return $"[{idCardTitle}] ";
    }

    private string GetIdCardColor(EntityUid senderUid)
    {
        var color = GetIdCard(senderUid)?.JobColor;
        return (!string.IsNullOrEmpty(color)) ? color : "#9FED58";
    }

    private string GetIdSprite(EntityUid senderUid)
    {
        if (HasComp<BorgChassisComponent>(senderUid))
            return BorgIconPath;

        if (HasComp<StationAiHeldComponent>(senderUid))
            return StationAiIconPath;

        var protoId = GetIdCard(senderUid)?.JobIcon;
        var sprite = NoIdIconPath;

        if (_prototype.TryIndex(protoId, out var prototype))
        {
            switch (prototype.Icon)
            {
                case SpriteSpecifier.Texture tex:
                    sprite = tex.TexturePath.CanonPath;
                    break;
                case SpriteSpecifier.Rsi rsi:
                    sprite = rsi.RsiPath.CanonPath + "/" + rsi.RsiState + ".png";
                    break;
            }
        }

        return sprite;
    }

    private bool GetIdCardIsBold(EntityUid senderUid)
    {
        return GetIdCard(senderUid)?.RadioBold ?? false;
    }
    // Sunrise-End

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
