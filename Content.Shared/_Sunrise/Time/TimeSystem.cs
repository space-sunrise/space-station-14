using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;
using System;

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
            var moscowTime = DateTime.UtcNow + TimeSpan.FromHours(3);
            var stationTime = moscowTime.TimeOfDay;

            var daysPassed = (int)stationTime.TotalHours / 24;
            stationTime = stationTime.Subtract(TimeSpan.FromHours(daysPassed * 24));
            
            var date = 13 + daysPassed;

            return (stationTime, date);
        }

        public string GetDate()
        {
            return DateTime.Now.AddYears(1000).ToString("dd.MM.yyyy");
        }
}