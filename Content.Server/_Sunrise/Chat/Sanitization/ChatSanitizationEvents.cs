using Content.Shared.Chat;

namespace Content.Server._Sunrise.Chat.Sanitization;

[ByRefEvent]
public record struct TrySendChatMessageEvent(
    string Message,
    InGameICChatType? IcChatType = null,
    InGameOOCChatType? OocChatType = null,
    bool ProcessUserInput = true,
    bool Cancelled = false);
