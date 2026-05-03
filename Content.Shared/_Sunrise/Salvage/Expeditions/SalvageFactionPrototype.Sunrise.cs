using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions;

public sealed partial class SalvageFactionPrototype
{
    [DataField]
    public List<ProtoId<SalvageDifficultyPrototype>>? Difficulties { get; private set; }
}
