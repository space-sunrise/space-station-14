using Content.Server.Research.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server._Sunrise.Research.Components;

[RegisterComponent, Access(typeof(ResearchSystem))]
public sealed partial class ResearchPopulationComponent : Component
{
    [DataField]
    public float Weight = 1f;
}
