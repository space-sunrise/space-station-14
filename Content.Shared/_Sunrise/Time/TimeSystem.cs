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

        public (TimeSpan Time, int Date) GetStationTime()
        {
            var stationTime = _timing.CurTime.Subtract(_roundStart).Add(TimeSpan.FromHours(12));

            var date = 13;
            while (stationTime.TotalHours >= 24)
            {
                stationTime.Subtract(TimeSpan.FromHours(24));
                date = date + 1;
            }

            return (stationTime, date);
        }

        public string GetDate()
        {
            // please tell me you guys aren't gonna have a 4 week round yet...
            return Loc.GetString("standard-date", ("date", GetStationTime().Date));
        }
    }
}