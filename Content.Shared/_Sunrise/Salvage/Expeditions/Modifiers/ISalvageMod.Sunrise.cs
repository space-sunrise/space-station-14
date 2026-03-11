using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

public partial interface ISalvageMod
{
    List<ProtoId<SalvageDifficultyPrototype>>? Difficulties { get; }
}
