using Content.Shared.Clothing.Dirt;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Clothing.Dirt;

// рисует цветной полупрозрачный квадрат поверх спрайта одежды в мире
public sealed class ClothingDirtOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ClothingDirtOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var bounds = args.WorldBounds;

        var query = _ent.EntityQueryEnumerator<ClothingDirtComponent, TransformComponent>();
        while (query.MoveNext(out _, out var dirt, out var xform))
        {
            if (dirt.DirtLevel <= 0f)
                continue;

            var pos = xform.WorldPosition;
            if (!bounds.Contains(pos))
                continue;

            // прозрачность = уровень / 100 но не больше 0.6 чтобы спрайт не перекрывался
            var alpha = dirt.DirtLevel / 100f * 0.6f;
            handle.DrawRect(Box2.CenteredAround(pos, Vector2.One), dirt.DirtColor.WithAlpha(alpha));
        }
    }
}
