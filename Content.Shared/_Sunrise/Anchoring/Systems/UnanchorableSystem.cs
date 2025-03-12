using Content.Shared._Sunrise.Anchoring.Components;
using Content.Shared.Construction.Components;

namespace Content.Shared._Sunrise.Anchoring.Systems;

public sealed class UnAnchorableSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UnanchorableComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    private void OnUnanchorAttempt(EntityUid uid, UnanchorableComponent component, UnanchorAttemptEvent args)
    {
        args.Cancel();
    }
}
