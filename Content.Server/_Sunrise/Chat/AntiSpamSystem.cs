using System.Linq;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.AntiSpam;

public sealed class AntiSpamSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    private readonly Dictionary<NetUserId, List<(string Message, float Time)>> _messageHistory = new();
    private const float SpamWindow = 3f;
    private const int MaxSameMessageCount = 3;
    private const int MaxPlayerMessageHistorySize = 6;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, TrySendICMessageEvent>(SpamICCheck);
    }


    private void SpamICCheck(EntityUid uid, MobStateComponent component, ref TrySendICMessageEvent args)
    {
        if (args.Player == null)
            return;

        if (args.DesiredType == InGameICChatType.Emote) // ignore emote chat
            return;

        var now = (float)_timing.CurTime.TotalSeconds;

        if (!_messageHistory.TryGetValue(args.Player.UserId, out var history))
        {
            // список с предложенной емкостью, чтобы уменьшить переаллокации.
            history = new List<(string Message, float Time)>();
            _messageHistory[args.Player.UserId] = history;
        }

        // Cleaning up old records (older than 5 seconds)
        history.RemoveAll(m => now - m.Time > 5f);
        var currentMessage = args.Message;

        // Add current message
        history.Add((currentMessage, now));



        // Count repetitions for the last 1.5 and 5 seconds
        int repeatsInShort = history.Count(m => m.Message == currentMessage && now - m.Time <= 1.5f);
        int repeatsInLong = history.Count(m => m.Message == currentMessage);

        if (repeatsInShort > 1 || repeatsInLong > 2)
        {
            history.Clear(); // reset spam history
            args.Cancel();

            var selfMessage = Loc.GetString("spam-mute-text-self");
            _popup.PopupEntity(selfMessage, uid, PopupType.Large);

            _statusEffects.TryAddStatusEffect<MutedComponent>(uid, "Muted", TimeSpan.FromSeconds(300), true);

            return;
        }
    }

}

public sealed class TrySendICMessageEvent(string message, InGameICChatType desiredType, ICommonSession? player = null) : CancellableEntityEventArgs
{
    public readonly string Message = message;
    public readonly InGameICChatType DesiredType = desiredType;
    public readonly ICommonSession? Player = player;
}
