namespace Content.Server._Sunrise.StationCentComm;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CentCommStationComponent : Component
{
    [DataField(readOnly: true)]
    public EntityUid ParentStation;
}
