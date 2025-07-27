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
public sealed class PrinterJobView(string title, PrinterJobType type)
{
    public readonly string Title = title;
    public readonly  PrinterJobType Type = type;

    public override string ToString()
    {
       return Type switch
       {
           PrinterJobType.Print => $"{Loc.GetString("printerdoc-print-job")}: {Title}",
           PrinterJobType.Copy => $"{Loc.GetString("printerdoc-copy-job")}: {Title}",
           _ => Title
       };
    }
}

[Serializable, NetSerializable]
public enum PrinterDocUiKey : byte
{
    Key
}
