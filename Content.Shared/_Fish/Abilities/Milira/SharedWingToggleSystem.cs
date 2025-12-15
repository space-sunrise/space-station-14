using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared._Fish.Abilities.Milira;

namespace Content.Shared._Fish.Abilities.Milira;

/// <summary>
/// Shared система для блокировки одевания одежды при раскрытых крыльях.
/// </summary>
public sealed class SharedWingToggleSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingToggleComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(EntityUid uid, WingToggleComponent component, ref IsEquippingAttemptEvent args)
    {
        if (!component.WingsOpened)
            return;

        if (args.Slot != "outerClothing")
            return;

        if (component.AllowedTag != null && _tagSystem.HasTag(args.Equipment, component.AllowedTag.Value))
            return;

        args.Cancel();
    }
}

