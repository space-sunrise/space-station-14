namespace Content.Shared._Sunrise.CentCom;

[RegisterComponent]
public sealed partial class CentComCargoConsoleComponent : Component
{
    [DataField]
    public CargoLinkedStation? LinkedStation;
}
