using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._Sunrise.Chat.Sanitization;

public sealed partial class ChatSanitizationSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly Dictionary<NetUserId, List<(string Message, float Time)>> MessageHistory = new();

    private bool _antiSpamEnabled;
    private int _counterShort;
    private int _counterLong;
    private float _muteDuration;
    private float _timeShort;
    private float _timeLong;

    private void InitializeAntiSpam()
    {
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartHistoryCleanup);

        // Получаем значения сразу, чтобы система работала при изменении сиваров через конфиг
        _antiSpamEnabled = _configuration.GetCVar(SunriseCCVars.AntiSpamEnable);
        _counterShort = _configuration.GetCVar(SunriseCCVars.AntiSpamCounterShort);
        _counterLong = _configuration.GetCVar(SunriseCCVars.AntiSpamCounterLong);
        _muteDuration = _configuration.GetCVar(SunriseCCVars.AntiSpamMuteDuration);
        _timeShort = _configuration.GetCVar(SunriseCCVars.AntiSpamTimeShort);
        _timeLong = _configuration.GetCVar(SunriseCCVars.AntiSpamTimeLong);

        _configuration.OnValueChanged(SunriseCCVars.AntiSpamEnable, val => _antiSpamEnabled = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamCounterShort, val => _counterShort = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamCounterLong, val => _counterLong = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamMuteDuration, val => _muteDuration = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamTimeShort, val => _timeShort = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamTimeLong, val => _timeLong = val, true);
    }

    private void SpamCheck(Entity<ActorComponent> ent, ref TrySendChatMessageEvent args)
    {
        if (ShouldSkipSpamCheck(args.IcChatType))
            return;

        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        var now = (float)_timing.CurTime.TotalSeconds;
        var history = GetMessageHistory(session.UserId);

        CleanOldMessages(history, now);
        history.Add((args.Message, now));

        var (repeatsShort, repeatsLong) = GetMessageRepeatCounts(history, args.Message, now);

        if (repeatsShort > _counterShort || repeatsLong > _counterLong)
        {
            ApplyMuteForSpam(ent, ref args, history);
        }
    }

    private bool ShouldSkipSpamCheck(InGameICChatType? type)
    {
        if (!_antiSpamEnabled)
            return true;

        if (type == InGameICChatType.Emote)
            return true;

        return false;
    }

    private static List<(string Message, float Time)> GetMessageHistory(NetUserId userId)
    {
        if (MessageHistory.TryGetValue(userId, out var history))
            return history;

        history = [];
        MessageHistory[userId] = history;

        return history;
    }

    private void CleanOldMessages(List<(string Message, float Time)> history, float now)
    {
        history.RemoveAll(m => now - m.Time > _timeLong);
    }

    private (int repeatsShort, int repeatsLong) GetMessageRepeatCounts(List<(string Message, float Time)> history, string message, float now)
    {
        var repeatsShort = history.Count(m => m.Message == message && now - m.Time <= _timeShort);
        var repeatsLong = history.Count(m => m.Message == message);
        return (repeatsShort, repeatsLong);
    }

    private void ApplyMuteForSpam(EntityUid uid, ref TrySendChatMessageEvent args, List<(string, float)> history)
    {
        history.Clear();
        args.Cancel();

        var message = Loc.GetString("spam-mute-text", ("target", uid));
        _popup.PopupEntity(message, uid, PopupType.Large);

        _statusEffects.TryAddStatusEffect<MutedComponent>(uid, "Muted", TimeSpan.FromSeconds(_muteDuration), true);
    }

    private static void RoundRestartHistoryCleanup(RoundRestartCleanupEvent ev)
    {
        MessageHistory.Clear();
    }
}
