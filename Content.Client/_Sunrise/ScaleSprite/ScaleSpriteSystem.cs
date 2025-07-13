using System.Numerics;
using Content.Shared._Sunrise.ScaleSprite;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.ScaleSprite;

public sealed class ScaleVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScaleSpriteComponent, AfterAutoHandleStateEvent>(OnComponentInit);
    }

    private void OnComponentInit(Entity<ScaleSpriteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetScale((ent.Owner, sprite), sprite.Scale * ent.Comp.Scale);
    }
}
