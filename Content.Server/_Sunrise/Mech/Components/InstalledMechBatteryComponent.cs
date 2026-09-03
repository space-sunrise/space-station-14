namespace Content.Server._Sunrise.Mech.Components;

/// <summary>
/// Отмечает батарею, установленную в мех, и хранит связь для синхронизации энергии.
/// </summary>
[RegisterComponent]
public sealed partial class InstalledMechBatteryComponent : Component
{
    /// <summary>
    /// Мех, использующий эту батарею.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Mech;

    /// <summary>
    /// Время следующей синхронизации вычисляемого заряда.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUpdate;
}
