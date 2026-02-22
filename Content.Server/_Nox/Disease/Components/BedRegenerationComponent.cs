// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class BedRegenerationComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public BedRegenerationType RegenerationType = BedRegenerationType.Normal;
}
