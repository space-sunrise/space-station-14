namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneSummoningProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? BaseRune;
}
