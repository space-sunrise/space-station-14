// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Content.Shared._Sunrise.PlaytimeTop;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.PlaytimeTop;

/// <summary>
/// Клиентский менеджер топа игроков по онлайну.
/// Получает данные от сервера и уведомляет UI о обновлениях.
/// </summary>
public sealed class PlaytimeTopManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public List<PlaytimeTopEntry> OnlineNow { get; private set; } = [];
    public List<PlaytimeTopEntry> ActiveWeek { get; private set; } = [];
    public List<PlaytimeTopEntry> ActiveMonth { get; private set; } = [];
    public List<PlaytimeTopEntry> AllTime { get; private set; } = [];

    /// <summary>Вызывается при получении обновлённых данных топа от сервера.</summary>
    public event Action? PlaytimeTopChanged;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgPlaytimeTop>(OnPlaytimeTopReceived);
    }

    private void OnPlaytimeTopReceived(MsgPlaytimeTop msg)
    {
        OnlineNow = msg.OnlineNow;
        ActiveWeek = msg.ActiveWeek;
        ActiveMonth = msg.ActiveMonth;
        AllTime = msg.AllTime;
        PlaytimeTopChanged?.Invoke();
    }
}
