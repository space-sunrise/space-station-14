using Content.Shared._Sunrise.ServersHub;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.ServersHub;

public partial class ServersHubManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] protected readonly IUserInterfaceManager UIManager = default!;

    public event Action<List<ServerHubEntry>>? ServersDataListChanged;

    public List<ServerHubEntry> ServersDataList = [];

    private ServersHubUi? _window;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgFullServerHubList>(OnServersDataChanged);
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCentered();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<ServersHubUi>();
    }

    private void OnServersDataChanged(MsgFullServerHubList msg)
    {
        // Храним всю инфу внутри данного менеджера чтобы даже если конект с сервером будет разорван у нас работал хаб,
        // и его можно было отобразить после отключения или бана.
        ServersDataList = msg.ServersHubEntries;
        ServersDataListChanged?.Invoke(ServersDataList);
        _window!.RefreshHeader();
    }
}
