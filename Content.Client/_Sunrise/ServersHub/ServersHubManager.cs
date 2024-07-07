using System.Linq;
using Content.Shared._Sunrise.ServersHub;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.ServersHub;

public partial class ServersHubManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public event Action<List<ServerHubEntry>>? ServersDataListChanged;

    public List<ServerHubEntry> ServersDataList = [];

    private ServersHubUi? _menu;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgFullServerHubList>(OnServersDataChanged);

        _menu = new ServersHubUi();
    }

    // Ахуеть какой костыль, но явно лучше чем писать лишние 300 строк.
    public void OpenServersHub()
    {
        _menu = new ServersHubUi();
        _menu.OnClose += _menu.Close;
        _menu.OpenCentered();
        var totalPlayers = ServersDataList.Sum(server => server.CurrentPlayers);
        var maxPlayers = ServersDataList.Sum(server => server.MaxPlayers);
        _menu!.RefreshHeader(totalPlayers, maxPlayers);
    }

    private void OnServersDataChanged(MsgFullServerHubList msg)
    {
        // Храним всю инфу внутри данного менеджера чтобы даже если конект с сервером будет разорван у нас работал хаб,
        // и его можно было отобразить после отключения или бана.
        ServersDataList = msg.ServersHubEntries;
        ServersDataListChanged?.Invoke(ServersDataList);
        var totalPlayers = ServersDataList.Sum(server => server.CurrentPlayers);
        var maxPlayers = ServersDataList.Sum(server => server.MaxPlayers);
        _menu!.RefreshHeader(totalPlayers, maxPlayers);
    }
}
