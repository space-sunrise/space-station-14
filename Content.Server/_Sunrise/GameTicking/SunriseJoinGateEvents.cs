using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.GameTicking;

public sealed class SunriseJoinGateConnectedEvent : EntityEventArgs
{
    public SunriseJoinGateConnectedEvent(ICommonSession session)
    {
        Session = session;
    }

    public ICommonSession Session { get; }
    public Task<bool>? GateTask { get; private set; }

    public void HandleWith(Task<bool> gateTask)
    {
        GateTask = gateTask;
    }
}

public sealed class SunriseJoinGateEnterGameEvent : EntityEventArgs
{
    public SunriseJoinGateEnterGameEvent(ICommonSession session)
    {
        Session = session;
    }

    public ICommonSession Session { get; }
    public bool Cancelled { get; private set; }

    public void Cancel()
    {
        Cancelled = true;
    }
}
