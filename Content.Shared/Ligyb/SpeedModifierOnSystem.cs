// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Item;

/// <summary>
/// This handles <see cref="HeldSpeedModifierComponent"/>
/// </summary>
public sealed class SpeedModifierOnSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SpeedModifierOnComponent, ComponentInit>(OnGotEquippedHand);
        SubscribeLocalEvent<SpeedModifierOnComponent, ComponentShutdown>(OnGotUnequippedHand);
        SubscribeLocalEvent<SpeedModifierOnComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnGotEquippedHand(EntityUid uid, SpeedModifierOnComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnGotUnequippedHand(EntityUid uid, SpeedModifierOnComponent component, ComponentShutdown args)
    {
        component.TurnedOff = true;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, SpeedModifierOnComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.TurnedOff)
        {
            args.ModifySpeed(component.WalkModifier, component.SprintModifier);
        }
    }
}
