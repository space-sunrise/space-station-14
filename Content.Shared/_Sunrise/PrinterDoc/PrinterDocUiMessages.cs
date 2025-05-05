using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PrinterDoc;

[Serializable, NetSerializable]
public sealed class PrinterDocPrintMessage(string templateId) : BoundUserInterfaceMessage
{
    public string TemplateId { get; } = templateId;
}

[Serializable, NetSerializable]
public sealed class PrinterDocCopyMessage() : BoundUserInterfaceMessage
{
}
