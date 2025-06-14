using System.Linq;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.AntiSpam;

public sealed class AntiSpamSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private static readonly Dictionary<NetUserId, List<(string Message, float Time)>> MessageHistory = new();
    private EntityQuery<HumanoidAppearanceComponent> _humanoidQuery;

    private bool _enabled; //eneble-disable mute system
    private int _counterShort; // allowed number of messages for TimeShort duration
    private int _counterLong; // allowed number of messages for TimeLong duration
    private float _muteDuration; // mute time
    private float _timeShort; // minimum check time
    private float _timeLong; // maximum check time

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, TrySendICMessageEvent>(SpamICCheck);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartHistoryCleanup);
        _humanoidQuery = GetEntityQuery<HumanoidAppearanceComponent>();
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamEnable, enabled => _enabled = enabled, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamCounterShort, val => _counterShort = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamCounterLong, val => _counterLong = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamMuteDuration, val => _muteDuration = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamTimeShort, val => _timeShort = val, true);
        _configuration.OnValueChanged(SunriseCCVars.AntiSpamTimeLong, val => _timeLong = val, true);
    }

    private void SpamICCheck(Entity<MobStateComponent> ent, ref TrySendICMessageEvent args)
    {
        if (!_enabled)
            return;
        if (args.Player == null)
            return;
        if (!_humanoidQuery.HasComp(ent.Owner))
            return;
        if (args.DesiredType == InGameICChatType.Emote) // ignore emote chat
            return;

        var now = (float)_timing.CurTime.TotalSeconds;

        if (!MessageHistory.TryGetValue(args.Player.UserId, out var history))
        {
            history = new List<(string Message, float Time)>();
            MessageHistory[args.Player.UserId] = history;
        }

        // Cleaning up old records (older than 5 seconds)
        history.RemoveAll(m => now - m.Time > _timeLong);
        var currentMessage = args.Message;

        // Add current message
        history.Add((currentMessage, now));

        // Count repetitions for the last 1.5 and 5 seconds
        var repeatsInShort = history.Count(m => m.Message == currentMessage && now - m.Time <= _timeShort);
        var repeatsInLong = history.Count(m => m.Message == currentMessage);

        if (repeatsInShort > _counterShort || repeatsInLong > _counterLong)
        {
            history.Clear(); // reset spam history
            args.Cancel();

            var selfMessage = Loc.GetString("spam-mute-text-self");
            _popup.PopupEntity(selfMessage, ent, PopupType.Large);

            _statusEffects.TryAddStatusEffect<MutedComponent>(ent, "Muted", TimeSpan.FromSeconds(_muteDuration), true);
        }
    }
    private void RoundRestartHistoryCleanup(RoundRestartCleanupEvent ev)
    {
        MessageHistory.Clear();
    }
}

public sealed class TrySendICMessageEvent(string message, InGameICChatType desiredType, ICommonSession? player = null) : CancellableEntityEventArgs
{
    public readonly string Message = message;
    public readonly InGameICChatType DesiredType = desiredType;
    public readonly ICommonSession? Player = player;
}
