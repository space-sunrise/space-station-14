using Content.Shared.Movement.Components;
using Content.Shared._Sunrise.Silicons.Borgs;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Borgs;

/// <summary>
/// Client side logic for borg type switching. Sets up primarily client-side visual information.
/// </summary>
/// <seealso cref="SharedBorgSwitchableTypeSystem"/>
/// <seealso cref="BorgSwitchableTypeComponent"/>
public sealed class BorgSwitchableTypeSystem : SharedBorgSwitchableTypeSystem
{
    [Dependency] private readonly BorgSystem _borgSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedBorgGenderSystem _borgGender = default!; // Sunrise-Edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableTypeComponent, AfterAutoHandleStateEvent>(AfterStateHandler);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BorgGenderComponent, AfterAutoHandleStateEvent>(OnBorgGenderState); // Sunrise-Edit
    }

    private void OnComponentStartup(Entity<BorgSwitchableTypeComponent> ent, ref ComponentStartup args)
    {
        RefreshEntityAppearance(ent.AsNullable());
    }

    private void AfterStateHandler(Entity<BorgSwitchableTypeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshEntityAppearance(ent.AsNullable());
    }

    // Sunrise-Edit
    private void OnBorgGenderState(Entity<BorgGenderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out BorgSwitchableTypeComponent? switchable))
            return;

        RefreshEntityAppearance((ent.Owner, switchable));
    }

    protected override void UpdateEntityAppearance(
        Entity<BorgSwitchableTypeComponent> entity,
        BorgTypePrototype prototype)
    {
        var visuals = _borgGender.ResolveVisuals(entity.Owner, prototype); // Sunrise-Edit
        if (TryComp(entity, out SpriteComponent? sprite))
        {
            _sprite.LayerSetData((entity, sprite), BorgVisualLayers.Body, visuals.Body); // Sunrise-Edit
            _sprite.LayerSetData((entity, sprite), BorgVisualLayers.LightStatus, visuals.ToggleLight); // Sunrise-Edit
        }

        if (TryComp(entity, out BorgChassisComponent? chassis))
        {
            _borgSystem.SetMindStates(
                (entity.Owner, chassis),
                visuals.HasMind,
                visuals.NoMind); // Sunrise-Edit

            if (TryComp(entity, out AppearanceComponent? appearance))
            {
                // Queue update so state changes apply.
                _appearance.QueueUpdate(entity, appearance);
            }
        }

        base.UpdateEntityAppearance(entity, prototype);
    }
}
