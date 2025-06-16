namespace Content.Server._Sunrise.ChatSan;

[ByRefEvent]
public record struct ChatSanRequestEvent
{
    public string Message;

    public bool Cancelled;
    public bool Handled;

    public ChatSanRequestEvent(string message)
    {
        Message = message;
    }
}
