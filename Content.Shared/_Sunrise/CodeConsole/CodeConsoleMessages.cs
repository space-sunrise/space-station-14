using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CodeConsole;

[Serializable, NetSerializable]
public sealed class CodeConsoleKeypadMessage : BoundUserInterfaceMessage
{
    public int Value;

    public CodeConsoleKeypadMessage(int value)
    {
        Value = value;
    }
}

[Serializable, NetSerializable]
public sealed class CodeConsoleKeypadEnterMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class CodeConsoleKeypadClearMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class CodeConsoleActivateButtonMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class CodeConsoleLockButtonMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class CodeConsoleUiState : BoundUserInterfaceState
{
    public bool IsLocked;
    public int EnteredCodeLength;
    public int MaxCodeLength;
}

[Serializable, NetSerializable]
public enum CodeConsoleUiKey : byte
{
    Key
}
