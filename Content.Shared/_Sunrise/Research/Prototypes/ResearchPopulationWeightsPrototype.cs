using System.Collections.Generic;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Research.Prototypes;

[Prototype]
public sealed partial class ResearchPopulationWeightsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, float> Weights = [];
}
