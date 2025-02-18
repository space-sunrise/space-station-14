using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14._Sunrise.Explosion;

// Омг это же партикл систем за 1$
public sealed class RMCExplosionSystem : SharedRMCExplosionSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpriteComponent, ExplosionSmokeEffectComponent>();

        while (query.MoveNext(out _, out var sprite, out _))
        {
            sprite.Offset += new Vector2(0.0012f, 0.0014f);
            sprite.Color = MakeColorMoreTransparent(sprite.Color);
        }
    }

    private static Color MakeColorMoreTransparent(Color color)
    {
        var newColorA = Math.Clamp(color.A - 0.001f, 0f, 255f);

        return new Color(color.R, color.G, color.B, newColorA);
    }
}
