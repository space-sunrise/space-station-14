using Content.Shared.Input;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Standing;

public sealed class StandingStateSystem : SharedStandingStateSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding, InputCmdHandler.FromDelegate(ToggleStanding))
            .Register<SharedStandingStateSystem>();

        SubscribeLocalEvent<RotationVisualsComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<StandingStateComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void ToggleStanding(ICommonSession? session)
    {
        if (session is not { AttachedEntity: { Valid: true } uid } _
            || !Exists(uid))
            return;

        RaiseNetworkEvent(new ChangeLayingDownEvent());
    }

    // Sunrise-Start
    private void OnAppearanceChanged(EntityUid uid, StandingStateComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        args.Sprite.DrawDepth = component.CurrentState == StandingState.Laying ? (int) DrawDepth.SmallMobs : (int) DrawDepth.Mobs;
    }
    // Sunrise-End

    private void OnMove(EntityUid uid, RotationVisualsComponent component, ref MoveEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<AppearanceComponent>(uid, out var appearance) ||
            !TryComp<TransformComponent>(uid, out var transform) ||
            component.DefaultRotation == 0)
            return;

        _appearance.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, appearance);

        var rotation = transform.LocalRotation + _eyeManager.CurrentEye.Rotation;
        if (rotation.GetDir() is Direction.East or Direction.North or Direction.NorthEast or Direction.SouthEast)
        {
            if (state != RotationState.Horizontal ||
                sprite.Rotation != component.DefaultRotation)
                return;

            component.HorizontalRotation = Angle.FromDegrees(270);
            sprite.Rotation = Angle.FromDegrees(270);

            return;
        }

        if (state != RotationState.Horizontal ||
            sprite.Rotation != Angle.FromDegrees(270))
            return;

        component.HorizontalRotation = component.DefaultRotation;

        sprite.Rotation = component.DefaultRotation;
    }
}
