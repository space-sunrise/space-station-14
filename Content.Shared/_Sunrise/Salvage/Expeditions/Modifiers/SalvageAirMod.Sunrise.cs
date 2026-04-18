using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

public sealed partial class SalvageAirMod
{
    [DataField]
    public List<ProtoId<SalvageDifficultyPrototype>>? Difficulties { get; private set; }
}
