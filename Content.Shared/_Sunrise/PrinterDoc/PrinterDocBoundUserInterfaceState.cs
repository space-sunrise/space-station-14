using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._Sunrise.PrinterDoc;

[Serializable, NetSerializable]
public sealed class PrinterDocBoundUserInterfaceState : BoundUserInterfaceState
{
    public int PaperCount { get; }
    public float InkAmount { get; }
    public List<string> Templates { get; }
    public bool CanCopy { get; }

    public PrinterDocBoundUserInterfaceState(
        int paperCount,
        float inkAmount,
        List<string> templates,
        bool canCopy)
    {
        PaperCount = paperCount;
        InkAmount = inkAmount;
        Templates = templates;
        CanCopy = canCopy;
    }
}

[Serializable, NetSerializable]
public enum PrinterDocUiKey : byte
{
    Key
}
