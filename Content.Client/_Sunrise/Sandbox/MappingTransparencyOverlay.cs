using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Sandbox;

public sealed class MappingTransparencyOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly SpriteSystem _sprite;

    private readonly List<(Entity<SpriteComponent> ent, float BaseAlpha)> _cachedBaseAlphas = new(256);

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public int TransparencyPercent { get; set; } = MappingTransparencySystem.DefaultTransparencyPercent;

    public MappingTransparencyOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _ent.System<SpriteSystem>();
    }

    public void ResetTransparency()
    {
        RestoreCachedTransparency();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        RestoreCachedTransparency();
        RefreshTransparency();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
    }

    private void RefreshTransparency()
    {
        var query = _ent.AllEntityQueryEnumerator<SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sprite, out var xform))
        {
            if (!xform.Anchored)
                continue;

            ApplyTransparency((uid, sprite));
        }
    }

    private void RestoreCachedTransparency()
    {
        foreach (var (ent, baseAlpha) in _cachedBaseAlphas)
        {
            RestoreTransparency(ent, baseAlpha);
        }

        _cachedBaseAlphas.Clear();
    }

    private void ApplyTransparency(Entity<SpriteComponent> ent)
    {
        var targetAlpha = ent.Comp.Color.A * (1f - TransparencyPercent / 100f);
        if (MathHelper.CloseTo(ent.Comp.Color.A, targetAlpha))
            return;

        _cachedBaseAlphas.Add((ent, ent.Comp.Color.A));
        _sprite.SetColor(ent.AsNullable(), ent.Comp.Color.WithAlpha(targetAlpha));
    }

    private void RestoreTransparency(Entity<SpriteComponent> ent, float baseAlpha)
    {
        if (MathHelper.CloseTo(ent.Comp.Color.A, baseAlpha))
            return;

        _sprite.SetColor(ent.AsNullable(), ent.Comp.Color.WithAlpha(baseAlpha));
    }
}
