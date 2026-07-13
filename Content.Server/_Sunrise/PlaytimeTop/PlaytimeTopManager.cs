// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Sunrise.PlaytimeTop;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.PlaytimeTop;

/// <summary>
/// Серверный менеджер топа игроков по онлайну.
/// Периодически обновляет данные из БД и рассылает их клиентам.
/// </summary>
public sealed class PlaytimeTopManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private const int TopCount = 10;
    private readonly TimeSpan _updateRate = TimeSpan.FromMinutes(1);
    private TimeSpan _nextUpdate = TimeSpan.Zero;

    // Кэш последнего рассчитанного топа
    private List<PlaytimeTopEntry> _cachedOnlineNow = [];
    private List<PlaytimeTopEntry> _cachedActiveWeek = [];
    private List<PlaytimeTopEntry> _cachedActiveMonth = [];
    private List<PlaytimeTopEntry> _cachedAllTime = [];

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("playtime_top");
        _netMgr.RegisterNetMessage<MsgPlaytimeTop>();
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            SendTo(e.Session);
        }
    }

    public void Update()
    {
        if (_nextUpdate > _timing.CurTime)
            return;

        _nextUpdate = _timing.CurTime + _updateRate;
        UpdateTopAsync();
    }

    private async void UpdateTopAsync()
    {
        try
        {
            await RefreshCaches();
            SendToAll();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Ошибка при обновлении топа игроков: {e}");
        }
    }

    private async Task RefreshCaches()
    {
        var cancel = CancellationToken.None;

        var daySince = DateTime.UtcNow - TimeSpan.FromDays(1);
        var dayData = await _db.GetTopPlayersActiveSinceWithSession(daySince, TopCount, cancel);
        _cachedOnlineNow = dayData
            .Select(x => new PlaytimeTopEntry(x.Username, x.Time))
            .ToList();

        var weekSince = DateTime.UtcNow - TimeSpan.FromDays(7);
        var weekData = await _db.GetTopPlayersActiveSinceWithSession(weekSince, TopCount, cancel);
        _cachedActiveWeek = weekData
            .Select(x => new PlaytimeTopEntry(x.Username, x.Time))
            .ToList();

        var monthSince = DateTime.UtcNow - TimeSpan.FromDays(30);
        var monthData = await _db.GetTopPlayersActiveSinceWithSession(monthSince, TopCount, cancel);
        _cachedActiveMonth = monthData
            .Select(x => new PlaytimeTopEntry(x.Username, x.Time))
            .ToList();

        var allTimeData = await _db.GetTopPlayersOverall(TopCount, cancel);
        _cachedAllTime = allTimeData
            .Select(x => new PlaytimeTopEntry(x.Username, x.Time))
            .ToList();
    }

    private void SendToAll()
    {
        var msg = BuildMessage();
        foreach (var session in _playerManager.Sessions)
        {
            _netMgr.ServerSendMessage(msg, session.Channel);
        }
    }

    private void SendTo(ICommonSession session)
    {
        var msg = BuildMessage();
        _netMgr.ServerSendMessage(msg, session.Channel);
    }

    private MsgPlaytimeTop BuildMessage()
    {
        return new MsgPlaytimeTop
        {
            OnlineNow = _cachedOnlineNow,
            ActiveWeek = _cachedActiveWeek,
            ActiveMonth = _cachedActiveMonth,
            AllTime = _cachedAllTime,
        };
    }
}
