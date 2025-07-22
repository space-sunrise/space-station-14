using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared._Sunrise.PrinterDoc;

[Serializable, NetSerializable]
public sealed class PrinterDocBoundUserInterfaceState : BoundUserInterfaceState
{
    public int PaperCount { get; }
    public float InkAmount { get; }
    public List<string> Templates { get; }
    public bool CanCopy { get; }

    public PrinterJobView? CurrentJob { get; }
    public List<PrinterJobView> Queue { get; }

    public PrinterDocBoundUserInterfaceState(
        int paperCount,
        float inkAmount,
        List<string> templates,
        bool canCopy,
        PrinterJobView? currentJob = null,
        List<PrinterJobView>? queue = null)
    {
        PaperCount = paperCount;
        InkAmount = inkAmount;
        Templates = templates;
        CanCopy = canCopy;
        CurrentJob = currentJob;
        Queue = queue ?? new();
    }
}

[Serializable, NetSerializable]
public sealed class PrinterJobView
{
    public string Title { get; }
    public PrinterJobType Type { get; }

    public PrinterJobView(string title, PrinterJobType type)
    {
        Title = title;
        Type = type;
    }

    public override string ToString()
    {
        return Type switch
        {
            PrinterJobType.Print => $"Печать документа: {Title}",
            PrinterJobType.Copy => $"Копирование: {Title}",
            _ => Title
        };
    }
}

[Serializable, NetSerializable]
public enum PrinterDocUiKey : byte
{
    Key
}
