using Content.Server.Examine;

namespace Content.Server._Sunrise.Chat;

public sealed class CheckVisibilitySystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VisibilityCheckEvent>(HandleVisibilityCheck);
    }

    private void HandleVisibilityCheck(VisibilityCheckEvent ev)
    {
        if (ev.Target is null)
        {
            ev.Cancel();
            return;
        }

        if (!_examineSystem.InRangeUnOccluded(ev.Source, ev.Target.Value, ev.Range))
        {
            ev.Cancel();
            return;
        }
    }
}
