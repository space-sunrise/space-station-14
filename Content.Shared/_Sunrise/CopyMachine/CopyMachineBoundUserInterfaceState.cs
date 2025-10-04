using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CopyMachine;

[Serializable, NetSerializable]
public sealed class CopyMachineBoundUserInterfaceState : BoundUserInterfaceState
{
    public int PaperCount { get; }
    public float InkAmount { get; }
    public List<string> Templates { get; }
    public bool CanCopy { get; }

    public CopyMachineJobView? CurrentJob { get; }
    public List<CopyMachineJobView> Queue { get; }

    public CopyMachineBoundUserInterfaceState(
        int paperCount,
        float inkAmount,
        List<string> templates,
        bool canCopy,
        CopyMachineJobView? currentJob = null,
        List<CopyMachineJobView>? queue = null)
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
public sealed class CopyMachineJobView
{
    public readonly string Title;
    public readonly CopyMachineJobType Type;
    public readonly string? TemplateId;

    public CopyMachineJobView(string title, CopyMachineJobType type, string? templateId = null)
    {
        Title = title;
        Type = type;
        TemplateId = templateId;
    }

    public override string ToString()
    {
       return Type switch
       {
           CopyMachineJobType.Print => $"{Loc.GetString("copy-machine-print-job")}: {Title}",
           CopyMachineJobType.Copy => $"{Loc.GetString("copy-machine-copy-job")}: {Title}",
           _ => Title
       };
    }
}

[Serializable, NetSerializable]
public enum CopyMachineUiKey : byte
{
    Key
}
