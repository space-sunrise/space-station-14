using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Time;

public sealed class TimeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _roundStart;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TickerLobbyStatusEvent>(LobbyStatus);
    }

        private void LobbyStatus(TickerLobbyStatusEvent ev)
        {
            _roundStart = ev.RoundStartTimeSpan;
        }

        public (TimeSpan Time, int Date) GetStationTime()
        {
			var moscowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
            var stationTime = moscowTime.TimeOfDay;

            var daysPassed = (int)stationTime.TotalHours / 24;
            stationTime = stationTime.Subtract(TimeSpan.FromHours(daysPassed * 24));
            
            var date = 13 + daysPassed;

            return (stationTime, date);
        }

    public string GetDate()
    {
        var moscowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
        return moscowTime.AddYears(1000).ToString("dd.MM.yyyy");
    }
}