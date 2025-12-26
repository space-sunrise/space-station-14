using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CommandConsole;


[Serializable, NetSerializable]
public sealed class CommandConsoleExecuteBoundUserInterfaceMessage : BoundUserInterfaceMessage // эти имена переменных меня убивают
{
    public string CommandInputText { get; set; } = string.Empty;

    public CommandConsoleExecuteBoundUserInterfaceMessage(string commandInputText)
    {
        CommandInputText = commandInputText;
    }
}
