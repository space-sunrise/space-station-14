using Content.Shared._Sunrise.ScaleSprite;
using System.Numerics;

namespace Content.Server._Sunrise.ScaleSprite;

public sealed class ScaleSpriteSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScaleSpriteComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, ScaleSpriteComponent component, ComponentInit args)
    {
        Scale(uid, component.Scale);
    }

    public bool Scale(EntityUid uid, Vector2 scale)
    {
        var component = EnsureComp<ScaleSpriteComponent>(uid);
        var appearance = EnsureComp<AppearanceComponent>(uid);
        if (_appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearance))
            _appearance.SetData(uid, ScaleSpriteVisuals.OldScale, oldScale, appearance);
        _appearance.SetData(uid, ScaleSpriteVisuals.Scale, scale, appearance);
        Dirty(uid, component);

        return true;
    }
}
