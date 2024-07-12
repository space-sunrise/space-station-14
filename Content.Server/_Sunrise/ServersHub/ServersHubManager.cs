using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Sunrise.ServersHub;
using Content.Shared._Sunrise.SunriseCCVars;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.ServersHub;

public sealed partial class ServersHubManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;

    private readonly List<ServerHubEntry> _serverDataList = new();

    private List<string> _serversList = new();

    private readonly HttpClient _httpClient = new();
    private ISawmill _sawmill = default!;

    private CancellationTokenSource _cts = new();

    // Не уверен что надо так часто это делать.
    private readonly TimeSpan _updateRate = TimeSpan.FromSeconds(15);

    private static readonly Regex RestrictedNameRegex = ServerNameRegex();

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("serversHub");

        _cfg.OnValueChanged(SunriseCCVars.ServersHubList, OnServerListChanged, true);

        Task.Run(async () => await PeriodicUpdateServerData(_cts.Token));

        _netMgr.RegisterNetMessage<MsgFullServerHubList>();
    }

    private void OnServerListChanged(string serverList)
    {
        var urls = new List<string>();

        foreach (var serverUrl in serverList.Split(','))
        {
            try
            {
                var uri = new Uri(serverUrl);
                urls.Add(uri.AbsoluteUri);
            }
            catch (UriFormatException)
            {
            }
        }

        _serversList = urls;
    }

    // Ахуенный план, надеждный блядь как швейцарские часы нахуй.
    private async Task PeriodicUpdateServerData(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UpdateServerData();
                await Task.Delay(_updateRate, token);
            }
        }
        catch (TaskCanceledException)
        {
            _sawmill.Info($"Task was cancelled");
        }
    }

    private async Task UpdateServerData()
    {
        if (_serversList.Count == 0)
            return;

        _cfg.SetCVar(CVars.ResourceUploadingLimitMb, 0f);

        _serverDataList.Clear();

        foreach (var serverUrl in _serversList)
        {
            var data = await RefreshServerData(serverUrl);
            if (data == null)
                continue;

            var thisServerUrl = _cfg.GetCVar(CVars.HubServerUrl);

            var canConnect = true;

            if (thisServerUrl != "")
                canConnect = NormalizeUrl(thisServerUrl) != NormalizeUrl(serverUrl);

            var serverName = RestrictedNameRegex.Replace(data.Name, string.Empty);
            _serverDataList.Add(new ServerHubEntry(
                serverName,
                data.Map ?? "",
                data.Preset ?? "",
                data.Players,
                data.Soft_Max_Players,
                $"ss14s://{NormalizeUrl(serverUrl)}",
                canConnect));
        }

        SendFullPlayerList(_playerManager.Sessions);
    }

    private string NormalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"{uri.Host}{uri.AbsolutePath.TrimEnd('/')}";
        }
        catch (UriFormatException)
        {
            _sawmill.Error($"Invalid URL format: {url}");
            return url;
        }
    }

    private async Task<ServerDataResponse?> RefreshServerData(string url, CancellationToken cancel = default)
    {
        using var resp = await _httpClient.GetAsync($"{url}/status", cancel);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            _sawmill.Error("SS14 server returned bad response {StatusCode}!", resp.StatusCode);
            return null;
        }

        var responseData = await resp.Content.ReadFromJsonAsync<ServerDataResponse>(cancellationToken: cancel);

        return responseData;
    }

    // Я в душе не ебу почему без _ оно не хочет парсить некоторые строки с json, но кому не похуй?
    [UsedImplicitly]
    private sealed record ServerDataResponse(
        string Name,
        string? Map,
        int Round_Id,
        int Players,
        int Soft_Max_Players,
        bool Panic_Bunker,
        int Run_Level,
        string? Preset,
        string? Round_Start_Time);

    private void SendFullPlayerList(ICommonSession[] sessions)
    {
        var netMsg = new MsgFullServerHubList();
        netMsg.ServersHubEntries = _serverDataList;

        foreach (var session in sessions)
        {
            try
            {
                _netMgr.ServerSendMessage(netMsg, session.Channel);
            }
            catch (Exception ex)
            {
                // Без данного Exception любые ошибки будут происходить абсолютно незаметно потому что вызывается это в асинхронном таске.
                _sawmill.Error($"Failed to send event to {session.Name}: {ex}");
            }
        }
    }

    [GeneratedRegex(@"[^А-Яа-яA-Za-zёЁ0-9' \-\.\[\]—:|]")]
    private static partial Regex ServerNameRegex();
}
