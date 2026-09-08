using Content.Client._Sunrise.Animations;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;

namespace Content.Client.Rotation;

public sealed class RotationVisualizerSystem : SharedRotationVisualsSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpritePoseSystem _pose = default!; // Sunrise-Edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RotationVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, RotationVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, args.Component))
            state = RotationState.Vertical;

        switch (state)
        {
            case RotationState.Vertical:
                AnimateSpriteRotation(uid, args.Sprite, component.VerticalRotation, component.AnimationTime);
                break;
            case RotationState.Horizontal:
                AnimateSpriteRotation(uid, args.Sprite, component.HorizontalRotation, component.AnimationTime);
                break;
        }
    }

    /// <summary>
    ///     Rotates a sprite between two animated keyframes given a certain time.
    /// </summary>
    public void AnimateSpriteRotation(EntityUid uid, SpriteComponent spriteComp, Angle rotation, float animationTime)
    {
        // Sunrise edit start - перенос в новую систему
        _pose.SetBasePose((uid, spriteComp), rotation, animationTime);
        // Sunrise edit end
    }
}
