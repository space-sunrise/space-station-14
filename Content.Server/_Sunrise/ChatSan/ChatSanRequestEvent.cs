namespace Content.Server._Sunrise.ChatSan;

[ByRefEvent]
public record struct ChatSanRequestEvent
{
    public EntityUid Source;
    public string Message;

    public bool Cancelled;
    public bool Handled;

    public ChatSanRequestEvent(EntityUid source, string message)
    {
        Source = source;
        Message = message;
    }
}
