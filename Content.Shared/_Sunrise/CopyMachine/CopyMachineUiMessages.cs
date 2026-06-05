using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CopyMachine;

[Serializable, NetSerializable]
public sealed class CopyMachinePrintMessage(string templateId) : BoundUserInterfaceMessage
{
    public string TemplateId { get; } = templateId;
}

[Serializable, NetSerializable]
public sealed class CopyMachineCopyMessage() : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CopyMachineCancelJobMessage(int index) : BoundUserInterfaceMessage
{
    public int Index { get; } = index;
}
