using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Salvage.Expeditions.Modifiers;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public sealed partial class SalvageWeatherMod
{
    [DataField]
    public List<ProtoId<SalvageDifficultyPrototype>>? Difficulties { get; private set; }
}
