namespace Content.Shared._Sunrise.CentCom;

[DataDefinition]
public sealed partial class CentComGift
{
    [DataField]
    public string Title = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public List<string> Contents = [];

    [DataField]
    public string Event = string.Empty;
}

[RegisterComponent]
public sealed partial class CentComCargoConsoleComponent : Component
{
    [DataField(readOnly: true)]
    public CargoLinkedStation? LinkedStation;

    [DataField]
    public List<CentComGift> Gifts = [];
}
