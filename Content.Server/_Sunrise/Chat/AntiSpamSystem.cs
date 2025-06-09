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

namespace Content.Server._Sunrise.AntiSpam;

public sealed class AntiSpamSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private static readonly Dictionary<NetUserId, List<(string Message, float Time)>> MessageHistory = new();

    private const int CounterShort = 1; // allowed number of messages for TimeShort duration
    private const int CounterLong = 2; // allowed number of messages for TimeLong duration
    private const int MuteDuration = 300; // mute time
    private const float TimeShort = 1.5f; // minimum check time
    private const float TimeLong = 5f; // maximum check time

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, TrySendICMessageEvent>(SpamICCheck);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartHistoryCleanup);
    }

    private void SpamICCheck(Entity<MobStateComponent> ent, ref TrySendICMessageEvent args)
    {
        if (args.Player == null)
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
        history.RemoveAll(m => now - m.Time > TimeLong);
        var currentMessage = args.Message;

        // Add current message
        history.Add((currentMessage, now));

        // Count repetitions for the last 1.5 and 5 seconds
        var repeatsInShort = history.Count(m => m.Message == currentMessage && now - m.Time <= TimeShort);
        var repeatsInLong = history.Count(m => m.Message == currentMessage);

        if (repeatsInShort > CounterShort || repeatsInLong > CounterLong)
        {
            history.Clear(); // reset spam history
            args.Cancel();

            var selfMessage = Loc.GetString("spam-mute-text-self");
            _popup.PopupEntity(selfMessage, ent, PopupType.Large);

            _statusEffects.TryAddStatusEffect<MutedComponent>(ent, "Muted", TimeSpan.FromSeconds(MuteDuration), true);
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
