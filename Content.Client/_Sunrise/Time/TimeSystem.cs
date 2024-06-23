namespace Content.Client._Sunrise.Time;

public sealed class TimeSystem : EntitySystem
{
    public (TimeSpan Time, int Date) GetStationTime()
    {
        var moscowTime = DateTime.UtcNow + TimeSpan.FromHours(3);
        var stationTime = moscowTime.TimeOfDay;

        var daysPassed = (int)stationTime.TotalHours / 24;
        stationTime = stationTime.Subtract(TimeSpan.FromHours(daysPassed * 24));

        var date = 13 + daysPassed;

        return (stationTime, date);
    }

    public string GetDate()
    {
        return DateTime.UtcNow.AddYears(1000).ToString("dd.MM.yyyy");
    }
}
