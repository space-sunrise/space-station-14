using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared._Sunrise.Proton;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Sunrise.Proton;

public struct ProtonRequest
{
    public NetUserId Admin { get; set; }
    public TimeSpan TimeStamp { get; set; }
}

public sealed class ProtonManager
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// Стэк реквестов,
    /// ключ - кому был дан запрос
    /// значение - от кого был запрос
    /// </summary>
    private Dictionary<NetUserId, ProtonRequest> _requests = new ();

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("proton");
        _sawmill.Debug($"Proton Manager successfully initialized");

        // Registration of messages
        _netManager.RegisterNetMessage<ProtonRequestScreenshotClient>();
        _netManager.RegisterNetMessage<ProtonRequestScreenshotServer>(HandleScreenshotRequest);
        _netManager.RegisterNetMessage<ProtonResponseScreenshotClient>(HandleScreenshotResponse);
        _netManager.RegisterNetMessage<ProtonResponseScreenshotServer>();
    }

    private void HandleScreenshotRequest(ProtonRequestScreenshotServer request)
    {
        var admin = _playerManager.GetSessionById(request.MsgChannel.UserId);
        if (!_adminManager.HasAdminFlag(admin, AdminFlags.Moderator))
        {
            _sawmill.Debug($"{request.MsgChannel.UserName} requested screenshot without proper admin flags");
            return;
        }

        if (request.Target == null)
        {
            _sawmill.Debug($"requested screenshot for null player");
            return;
        }

        var req = new ProtonRequestScreenshotClient();
        if (!_playerManager.TryGetSessionByUsername(request.Target, out var targetSession))
        {
            _sawmill.Debug($"requested screenshot for unexisting player");
            return;
        }

        if (_requests.ContainsKey(targetSession.UserId))
        {
            _sawmill.Error($"Multiple requests of one screenshot");
            return;
        }

        _netManager.ServerSendMessage(req, targetSession.Channel);

        _requests.Add(targetSession.UserId, new ProtonRequest() { Admin = admin.UserId, TimeStamp = _gameTiming.CurTime });

        _sawmill.Debug($"{request.MsgChannel.UserName} successfully requested screenshot of {targetSession.Channel.UserName}");

        Timer.Spawn(TimeSpan.FromSeconds(30), () => {CheckTimerOnFired(targetSession);});
    }

    private async void CheckTimerOnFired(ICommonSession session)
    {
        if (!_requests.ContainsKey(session.UserId))
            return;

        BanPlayer(session, "Это автоматический бан. Клиент не ответил на пакет в течении заданного времени");

        _sawmill.Debug($"Successfully banned {session.UserId} for not answering a packet");
    }

    private void BanPlayer(ICommonSession session, string message)
    {
        _console.ExecuteCommand($"ban {session.UserId} \"{message}\" 0 high");
    }

    private void HandleScreenshotResponse(ProtonResponseScreenshotClient response)
    {
        var session = _playerManager.GetSessionById(response.MsgChannel.UserId);

        if (!_requests.ContainsKey(response.MsgChannel.UserId))
        {
            _sawmill.Debug($"Received screenshot response without screenshot");
            BanPlayer(session, "Это автоматический бан. Клиент отправил пустой пакет.");
            return;
        }

        if (response.Screenshot == null)
        {
            _sawmill.Debug($"Received screenshot response with null screenshot");
            BanPlayer(session, "Это автоматический бан. Клиент отправил пустой пакет.");
            return;
        }

        if (response.Screenshot.Height > 5000 || response.Screenshot.Width > 5000)
        {
            _sawmill.Debug($"Received screenshot response with too big screenshot");
            BanPlayer(session, "Это автоматический бан. Клиент отправил слишком большой пакет.");
            return;
        }

        var entry = _requests[response.MsgChannel.UserId];

        var resp = new ProtonResponseScreenshotServer()
        {
            Screenshot = response.Screenshot,
        };

        var admin = _playerManager.GetSessionById(entry.Admin);
        _netManager.ServerSendMessage(resp, admin.Channel);

        _requests.Remove(response.MsgChannel.UserId);
    }
}
