using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.PrinterDoc;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrinterDocComponent : Component
{
    public const string CopySlotId = "paper_copy";

    [DataField(required: true)]
    public ItemSlot CopySlot = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string PaperMaterial = "Paper";

    [DataField]
    public EntProtoId PaperProtoId = "Paper";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string IncReagentProto = "Inc";

    [DataField]
    public string Solution = string.Empty;

    [DataField]
    public int IncCost = 5;

    [DataField]
    public int PaperCost = 100;

    [DataField]
    public List<ProtoId<DocTemplatePrototype>> Templates = new();
}
