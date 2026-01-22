using Content.Server.Mobs.Components;
using Content.Shared.Chat;

namespace Content.Server.Ghost;

public sealed partial class GhostSystem
{
    public void TrySendPendingLastWords(EntityUid entity)
    {
        if (!TryComp<PendingLastWordsComponent>(entity, out var pending))
            return;

        if (!_mobState.IsCritical(entity))
        {
            RemComp<PendingLastWordsComponent>(entity);
            return;
        }

        _chat.TrySendInGameICMessage(
            entity,
            pending.Text,
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal,
            checkRadioPrefix: false,
            ignoreActionBlocker: true);

        RemComp<PendingLastWordsComponent>(entity);
    }

    public void CancelPendingLastWords(EntityUid entity)
    {
        RemComp<PendingLastWordsComponent>(entity);
    }
}