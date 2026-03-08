using Content.Shared._Sunrise.DynamicAppearance;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Client-only guard for predicted DynamicAppearance UI opens.
/// Prevents the local BUI from opening when the actor is not allowed,
/// while the server still remains authoritative.
/// </summary>
public sealed class DynamicAppearanceUiValidationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicAppearanceComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
    }

    private void OnUiMessageAttempt(Entity<DynamicAppearanceComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.UiKey is not DynamicAppearanceUiKey.Key
            || args.Message is not OpenBoundInterfaceMessage)
        {
            return;
        }

        if (ent.Comp.AllowedFields == DynamicAppearanceFields.None || args.Actor != ent.Owner)
            args.Cancel();
    }
}
