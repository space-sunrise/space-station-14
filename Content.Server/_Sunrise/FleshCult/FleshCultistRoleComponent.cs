using Content.Shared.Roles.Components;

namespace Content.Server._Sunrise.FleshCult;

[RegisterComponent]
public sealed partial class FleshCultistRoleComponent : BaseMindRoleComponent
{
    [DataField("fleshHearts")]
    public int FleshHearts;
}
