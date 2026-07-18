using Robust.Shared.Player;

#pragma warning disable IDE0130
namespace Content.Server.Players.PlayTimeTracking;

public sealed partial class PlayTimeTrackingManager
{
    private static readonly TimeSpan PlayTimeSessionHeartbeatInterval = TimeSpan.FromMinutes(1);

    private readonly Dictionary<ICommonSession, TimeSpan> _playTimeSessionHeartbeatAt = new();

    private void UpdatePlayTimeSessions()
    {
        var time = _timing.RealTime;

        foreach (var (session, data) in _playTimeData)
        {
            if (!data.CurrentDbSessionId.HasValue)
                continue;

            if (!_playTimeSessionHeartbeatAt.TryGetValue(session, out var nextHeartbeat))
            {
                nextHeartbeat = time + PlayTimeSessionHeartbeatInterval;
                _playTimeSessionHeartbeatAt[session] = nextHeartbeat;
            }

            if (time < nextHeartbeat)
                continue;

            _playTimeSessionHeartbeatAt[session] = time + PlayTimeSessionHeartbeatInterval;
            TrackPending(_db.UpdatePlayTimeSessionAsync(data.CurrentDbSessionId.Value, DateTime.UtcNow));
        }
    }

    private void OnPlayTimeSessionStarted(ICommonSession session)
    {
        _playTimeSessionHeartbeatAt[session] = _timing.RealTime + PlayTimeSessionHeartbeatInterval;
    }

    private void OnPlayTimeSessionDisconnected(ICommonSession session)
    {
        _playTimeSessionHeartbeatAt.Remove(session);

        if (_playTimeData.TryGetValue(session, out var data) && data.CurrentDbSessionId.HasValue)
        {
            TrackPending(_db.UpdatePlayTimeSessionAsync(data.CurrentDbSessionId.Value, DateTime.UtcNow));
        }
    }
}
