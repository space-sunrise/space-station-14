using System.Linq;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs.Components;

namespace Content.Server._Sunrise.AntiSpam;

public sealed class ICSpamMessage : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly Dictionary<NetUserId, List<(string Message, float Time)>> _messageHistory = new();
    private const float SpamWindow = 3f;
    private const int MaxSameMessageCount = 3;
    const int MaxPlayerMessageHistorySize = 6;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, ChatSystem.TrySendICMessageEvent>(SpamICCheck);
    }


    private void SpamICCheck(EntityUid uid, MobStateComponent component, ref ChatSystem.TrySendICMessageEvent args)
    {
        if (args.Player != null)
        {
            if (args.DesiredType == InGameICChatType.Emote)
            {
                // ignore emote chat
            }
            else
            {

                var now = (float)_gameTiming.CurTime.TotalSeconds;

                if (!_messageHistory.TryGetValue(args.Player.UserId, out var history))
                {
                    // список с предложенной емкостью, чтобы уменьшить переаллокации.
                    history = new List<(string Message, float Time)>(capacity: MaxPlayerMessageHistorySize);
                    _messageHistory[args.Player.UserId] = history;
                }

                // Cleaning up old records (older than 5 seconds)
                history.RemoveAll(m => now - m.Time > 5f);
                var currentMessage = args.Message;

                // Add current message
                history.Add((currentMessage, now));

                if (history.Count > MaxPlayerMessageHistorySize)
                {
                    history.RemoveAt(0); // Удаляется самое старое сообщение
                }

                // Count repetitions for the last 1.5 and 5 seconds
                int repeatsInShort = history.Count(m => m.Message == currentMessage && now - m.Time <= 1.5f);
                int repeatsInLong = history.Count(m => m.Message == currentMessage);

                if (repeatsInShort > 1 || repeatsInLong > 2)
                {
                    history.Clear(); // reset spam history
                    args.Cancel();

                    var selfMessage = Loc.GetString("spam-mute-text-self");
                    _popupSystem.PopupEntity(selfMessage, args.Source, PopupType.Large);

                    var statusEffects = EntityManager.System<StatusEffectsSystem>();
                    statusEffects.TryAddStatusEffect<MutedComponent>(args.Source, "Muted", TimeSpan.FromSeconds(300), true);

                    return;
                }
            }
        }
    }

}
