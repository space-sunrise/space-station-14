namespace Content.Client._Sunrise.Time;

public sealed class TimeSystem : EntitySystem
{
    public TimeSpan GetTime()
    {
        return (DateTime.UtcNow + TimeSpan.FromHours(3)).TimeOfDay;
    }

    public string GetDate()
    {
        return (DateTime.UtcNow + TimeSpan.FromHours(3)).AddYears(1000).ToString("dd.MM.yyyy");
    }
}
