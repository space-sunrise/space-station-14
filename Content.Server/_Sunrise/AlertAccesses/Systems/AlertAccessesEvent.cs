/// <summary>
/// Проставил этот ивент во всех удовлетворяющих случаях смены кода.
/// </summary>
public sealed class AlertAccessesEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public AlertAccessesEvent(EntityUid station)
    {
        Station = station;
    }
}
