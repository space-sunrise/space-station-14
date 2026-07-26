using System.Linq;
using Content.Server._Sunrise.CollectiveMind;
using Content.Shared._Sunrise.CollectiveMind;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private readonly CollectiveMindSystem _collectiveMind = default!;

    /// <summary>
    /// Отправляет сообщение через общий IC-пайплайн чата
    /// </summary>
    public bool TrySendCollectiveMindMessage(
        EntityUid source,
        string message,
        ProtoId<CollectiveMindPrototype>? mind = null,
        IConsoleShell? shell = null,
        ICommonSession? player = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (mind is { } explicitMind)
        {
            if (!_prototypeManager.Resolve(explicitMind, out var prototype) || !_collectiveMind.TryResolveSender(source, prototype, out _))
                return false;

            message = $"{CollectiveMindPrefix}{prototype.KeyCode} {message}";
        }

        // Важно: санитизация сообщения происходит в TrySendInGameICMessage, поэтому здесь НЕ НУЖНО её делать
        TrySendInGameICMessage(
            source,
            message,
            InGameICChatType.CollectiveMind,
            ChatTransmitRange.Normal,
            shell: shell,
            player: player,
            checkRadioPrefix: false);
        return true;
    }

    private void SendCollectiveMindChat(EntityUid source, string message, CollectiveMindPrototype collectiveMind)
    {
        if (_mobStateSystem.IsDead(source) || string.IsNullOrEmpty(message))
            return;

        if (!_collectiveMind.TryResolveSender(source, collectiveMind.ID, out var group))
            return;

        var clients = Filter.Empty();
        var receivers = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<CollectiveMindComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out var memberCollectiveMind, out var actor))
        {
            if (_mobStateSystem.IsDead(uid))
                continue;

            if (!_collectiveMind.CanReceive((uid, memberCollectiveMind), collectiveMind, group))
                continue;

            clients.AddPlayer(actor.PlayerSession);
            receivers.Add(uid);
        }

        var admins = _adminManager.ActiveAdmins
            .Select(p => p.Channel);
        string messageWrap;
        string adminMessageWrap;

        if (collectiveMind.ShowAuthor)
        {
            messageWrap = Loc.GetString("collective-mind-chat-wrap-message-with-author",
                ("source", source),
                ("message", message),
                ("channel", collectiveMind.LocalizedName));
        }
        else
        {
            messageWrap = Loc.GetString("collective-mind-chat-wrap-message",
                ("message", message),
                ("channel", collectiveMind.LocalizedName));
        }

        adminMessageWrap = Loc.GetString("collective-mind-chat-wrap-message-admin",
            ("source", source),
            ("message", message),
            ("channel", collectiveMind.LocalizedName));

        var groupLog = group?.ToString() ?? "global";
        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"CollectiveMind {collectiveMind.ID} ({groupLog}) chat from {ToPrettyString(source):Player}: {message}");

        _chatManager.ChatMessageToManyFiltered(clients,
            ChatChannel.CollectiveMind,
            message,
            messageWrap,
            source,
            false,
            true,
            collectiveMind.Color);

        _chatManager.ChatMessageToMany(ChatChannel.CollectiveMind,
            message,
            adminMessageWrap,
            source,
            false,
            true,
            admins,
            collectiveMind.Color);

        RaiseLocalEvent(new CollectiveMindSpokeEvent(source, message, receivers, collectiveMind.ID));
    }

    private CollectiveMindPrototype? GetRedirectedCollectiveMind(EntityUid source, InGameICChatType desiredType)
    {
        if (desiredType is not (InGameICChatType.Speak or InGameICChatType.Whisper))
            return null;

        if (!_collectiveMind.TryGetRedirectedMind(source, out var mind) || !_prototypeManager.Resolve(mind, out var prototype))
            return null;

        return prototype;
    }

    private bool TryGetDefaultCollectiveMind(EntityUid source, out CollectiveMindPrototype collectiveMind)
    {
        collectiveMind = default!;
        if (!_collectiveMind.TryGetDefaultMind(source, out var mind) || !_prototypeManager.Resolve(mind, out var prototype))
            return false;

        collectiveMind = prototype;
        return true;
    }

    /// <summary>
    /// Gets all players who have working announcement speakers nearby.
    /// Used to filter chat recipients for announcements.
    /// </summary>
    private Filter GetPlayersWithWorkingSpeakers()
    {
        var filteredPlayers = Filter.Empty();

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            if (_announcementSpeaker.HasWorkingSpeakersNearby(playerEntity))
            {
                filteredPlayers = filteredPlayers.AddPlayer(player);
            }
        }

        return filteredPlayers;
    }

    /// <summary>
    /// Filters an existing filter to only include players with working speakers nearby.
    /// </summary>
    private Filter FilterPlayersByWorkingSpeakers(Filter originalFilter)
    {
        var filteredPlayers = Filter.Empty();

        foreach (var player in originalFilter.Recipients)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            if (_announcementSpeaker.HasWorkingSpeakersNearby(playerEntity))
            {
                filteredPlayers = filteredPlayers.AddPlayer(player);
            }
        }

        return filteredPlayers;
    }
}
