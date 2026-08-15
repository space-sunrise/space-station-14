using Robust.Shared.Player;

#pragma warning disable IDE0130
namespace Content.Server.Players.PlayTimeTracking;

public sealed partial class PlayTimeTrackingManager
{
    private static readonly TimeSpan PlayTimeSessionHeartbeatInterval = TimeSpan.FromMinutes(1);

    private void UpdatePlayTimeSessions()
    {
        var time = _timing.RealTime;

        foreach (var (session, data) in _playTimeData)
        {
            if (!data.CurrentDbSessionId.HasValue)
                continue;

            if (time < data.NextHeartbeat)
                continue;

            data.NextHeartbeat = time + PlayTimeSessionHeartbeatInterval;
            TrackPending(_db.UpdatePlayTimeSessionAsync(data.CurrentDbSessionId.Value, DateTime.UtcNow));
        }
    }

    private void OnPlayTimeSessionStarted(ICommonSession session)
    {
        if (_playTimeData.TryGetValue(session, out var data))
        {
            data.NextHeartbeat = _timing.RealTime + PlayTimeSessionHeartbeatInterval;
        }
    }

    private void OnPlayTimeSessionDisconnected(ICommonSession session)
    {
        if (_playTimeData.TryGetValue(session, out var data) && data.CurrentDbSessionId.HasValue)
        {
            TrackPending(_db.UpdatePlayTimeSessionAsync(data.CurrentDbSessionId.Value, DateTime.UtcNow));
        }
    }
}
