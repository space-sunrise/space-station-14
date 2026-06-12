using Content.Shared.Actions;
using Content.Shared._Sunrise.Silicons.Borgs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// Implements borg type switching.
/// </summary>
/// <seealso cref="BorgSwitchableTypeComponent"/>
public abstract partial class SharedBorgSwitchableTypeSystem : EntitySystem // Sunrise-Edit
{
    // TODO: Allow borgs to be reset to default configuration.

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;
    [Dependency] private readonly InteractionPopupSystem _interactionPopup = default!;
    [Dependency] private readonly SharedBorgGenderSystem _borgGender = default!; // Sunrise-Edit

    public static readonly EntProtoId ActionId = "ActionSelectBorgType";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableTypeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, BorgToggleSelectTypeEvent>(OnSelectBorgTypeAction);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, BorgGenderChangedEvent>(OnBorgGenderChanged); // Sunrise-Edit

        Subs.BuiEvents<BorgSwitchableTypeComponent>(BorgSwitchableTypeUiKey.SelectBorgType,
            sub =>
            {
                sub.Event<BorgSelectTypeMessage>(SelectTypeMessageHandler);
            });
    }

    //
    // UI-adjacent code
    //

    private void OnMapInit(Entity<BorgSwitchableTypeComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.SelectTypeAction, ActionId);
        Dirty(ent);

        if (ent.Comp.SelectedBorgType != null)
        {
            SelectBorgModule(ent, ent.Comp.SelectedBorgType.Value);
        }
    }

    private void OnShutdown(Entity<BorgSwitchableTypeComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SelectTypeAction);
    }

    private void OnSelectBorgTypeAction(Entity<BorgSwitchableTypeComponent> ent, ref BorgToggleSelectTypeEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;

        _userInterface.TryToggleUi((ent.Owner, null), BorgSwitchableTypeUiKey.SelectBorgType, actor.PlayerSession);
    }

    private void SelectTypeMessageHandler(Entity<BorgSwitchableTypeComponent> ent, ref BorgSelectTypeMessage args)
    {
        if (ent.Comp.SelectedBorgType != null)
            return;

        if (!Prototypes.HasIndex(args.Prototype))
            return;

        SelectBorgModule(ent, args.Prototype);
    }

    // Sunrise-Edit
    private void OnBorgGenderChanged(Entity<BorgSwitchableTypeComponent> ent, ref BorgGenderChangedEvent args)
    {
        RefreshEntityAppearance(ent.AsNullable());
    }

    //
    // Implementation
    //

    protected virtual void SelectBorgModule(
        Entity<BorgSwitchableTypeComponent> ent,
        ProtoId<BorgTypePrototype> borgType)
    {
        ent.Comp.SelectedBorgType = borgType;

        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SelectTypeAction);
        ent.Comp.SelectTypeAction = null;
        Dirty(ent);

        _userInterface.CloseUi((ent.Owner, null), BorgSwitchableTypeUiKey.SelectBorgType);

        RefreshEntityAppearance(ent.AsNullable());
    }

    public void RefreshEntityAppearance(Entity<BorgSwitchableTypeComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!Prototypes.Resolve(entity.Comp.SelectedBorgType, out var proto))
            return;

        UpdateEntityAppearance((entity.Owner, entity.Comp), proto);
    }

    protected virtual void UpdateEntityAppearance(
        Entity<BorgSwitchableTypeComponent> entity,
        BorgTypePrototype prototype)
    {
        if (TryComp(entity, out InteractionPopupComponent? popup))
        {
            _interactionPopup.SetInteractSuccessString((entity.Owner, popup), prototype.PetSuccessString);
            _interactionPopup.SetInteractFailureString((entity.Owner, popup), prototype.PetFailureString);
        }

        if (TryComp(entity, out FootstepModifierComponent? footstepModifier))
        {
            footstepModifier.FootstepSoundCollection = prototype.FootstepCollection;
        }

        var visuals = _borgGender.ResolveVisuals(entity.Owner, prototype); // Sunrise-Edit
        if (visuals.BodyMovement is { } movementLayer)
        {
            var spriteMovement = EnsureComp<SpriteMovementComponent>(entity);
            spriteMovement.NoMovementLayers.Clear();
            spriteMovement.NoMovementLayers["movement"] = visuals.Body;
            spriteMovement.MovementLayers.Clear();
            spriteMovement.MovementLayers["movement"] = movementLayer;
        }
        else
        {
            RemComp<SpriteMovementComponent>(entity);
        }
    }
}
