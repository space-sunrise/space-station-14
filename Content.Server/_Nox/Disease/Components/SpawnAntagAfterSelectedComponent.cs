// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Nox.Disease.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent, Access(typeof(SpawnAntagAfterSelectedRule))]
public sealed partial class SpawnAntagAfterSelectedComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId Prototype = "SentientDisease";
}
