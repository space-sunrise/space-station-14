using Content.Server.Chat.Systems;

namespace Content.Server._Sunrise.Chat.Sanitization;

public sealed class TrySendChatMessageEvent(string message, InGameICChatType? icChatType = null, InGameOOCChatType? oocChatType = null)
    : CancellableEntityEventArgs
{
    public string Message = message;
    public readonly InGameICChatType? IcChatType = icChatType;
    public readonly InGameOOCChatType? OocChatType = oocChatType;
}
