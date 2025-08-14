using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.PrinterDoc;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrinterDocComponent : Component
{
    /// <summary>
    /// Максимальное количество заданий в очереди принтера
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxQueueSize = 5;

    [DataField]
    public Content.Shared._Sunrise.PrinterDoc.PrinterJobView? CurrentJobView;

    [DataField]
    public SoundSpecifier PrintSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// Время выполнения одной печати, копии(в секундах)
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float JobDuration = 4f;

    /// <summary>
    /// Начальные материалы принтера - бумага
    /// </summary>
    [DataField]
    public int InitialPaperAmount = 3000;
    /// <summary>
    /// Начальные материалы принтера - чернила
    /// </summary>
    [DataField]
    public int InitialInkAmount = 30;

    /// <summary>
    /// Очередь заданий на печать или копирование
    /// </summary>
    public Queue<(PrinterJobType Type, string? TemplateId)> JobQueue = new();

    /// <summary>
    /// Идёт ли сейчас выполнение задания
    /// </summary>
    [ViewVariables]
    public bool IsProcessing = false;

    /// <summary>
    /// ID слота для копируемой бумаги
    /// </summary>
    public const string CopySlotId = "paper_copy";

    /// <summary>
    /// Слот для вставляемого листа бумаги, который можно скопировать
    /// </summary>
    [DataField(required: true)]
    public ItemSlot CopySlot = new();

    /// <summary>
    /// Прототип материала бумаги (используется для проверки количества бумаги в принтере)
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MaterialPrototype> PaperMaterial = "officePaper";

    /// <summary>
    /// Прототип сущности бумаги, которую принтер создаёт при печати
    /// </summary>
    [DataField]
    public EntProtoId PaperProtoId = "Paper";

    /// <summary>
    /// Прототип реагента чернил, используемых принтером
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> IncReagentProto = "Inc";

    /// <summary>
    /// Название раствора с чернилами внутри принтера
    /// </summary>
    [DataField]
    public string Solution = string.Empty;

    /// <summary>
    /// Сколько чернил тратится на печать одного листа
    /// </summary>
    [DataField]
    public int IncCost = 1;

    /// <summary>
    /// Сколько материала бумаги тратится на один лист (в условных единицах материала)
    /// </summary>
    [DataField]
    public int PaperCost = 100;

    /// <summary>
    /// Доступные шаблоны документов для печати
    /// </summary>
    [DataField]
    public List<ProtoId<DocTemplatePrototype>> Templates = new();

    [Serializable, NetSerializable]
    public sealed record PrinterJobView(string Type, string? TemplateId);

}
