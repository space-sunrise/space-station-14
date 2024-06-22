using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Time
{
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

        public (TimeSpan Time) GetStationTime()
        {
            var stationTime = _timing.CurTime.Subtract(_roundStart).Add(TimeSpan.FromHours(12));

            stationTime = stationTime.Subtract(TimeSpan.FromHours(daysPassed * 24));
			
            return (stationTime);
        }
		
        public TimeSpan GetCurrentServerTime()
        {
            var moscowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
            return moscowTime.TimeOfDay;
        }

        public string GetDate()
        {
            var moscowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
            return moscowTime.AddYears(1000).ToString("dd.MM.yyyy");
        }
    }
}