using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Item;

/// <summary>
/// Система для обработки логики обновления таймера у <see cref="ItemRepickupCooldownComponent"/>
/// </summary>
public sealed class ItemRepickupCooldownSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemRepickupCooldownComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<ItemRepickupCooldownComponent, ThrownEvent>(OnThrown);
    }

    private void OnThrown(EntityUid uid, ItemRepickupCooldownComponent component, ThrownEvent args)
    {
        component.PrevDrop = _timing.CurTime;
    }

    private void OnDropped(EntityUid uid, ItemRepickupCooldownComponent component, DroppedEvent args)
    {
        component.PrevDrop = _timing.CurTime;
    }
}
