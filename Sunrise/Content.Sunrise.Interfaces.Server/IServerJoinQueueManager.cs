using Robust.Shared.Player;
using System.Threading.Tasks;

namespace Content.Sunrise.Interfaces.Server;

public interface IServerJoinQueueManager
{
    public bool IsEnabled { get; }
    public int PlayerInQueueCount { get; }
    public int ActualPlayersCount { get; }
    public void Initialize();
    public void PostInitialize();
    public Task HandleReadyToJoin(ICommonSession session);
}
