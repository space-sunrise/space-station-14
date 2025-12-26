using System.Threading;
using Content.Shared.GameTicking;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Sunrise.Misc.TimedRemoveComponents;

public sealed class TimedRemoveComponentsSystem : EntitySystem
{
    private static CancellationTokenSource _timerDespawnToken = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedRemoveComponentsComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Clear());
    }

    private void OnInit(Entity<TimedRemoveComponentsComponent> ent, ref ComponentInit args)
    {
        Timer.Spawn(ent.Comp.RemoveAfter, () => RemoveComponents(ent), _timerDespawnToken.Token);
    }

    private void RemoveComponents(Entity<TimedRemoveComponentsComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        EntityManager.RemoveComponents(ent, ent.Comp.Components);

        // блять, я себя захуярил
        RemComp<TimedRemoveComponentsComponent>(ent);
    }

    private static void Clear()
    {
        _timerDespawnToken.Cancel();
        _timerDespawnToken = new();
    }
}
