using System.Numerics;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

// Файл намеренно расположен в _Sunrise, но расширяет ванильный partial-класс.
#pragma warning disable IDE0130
namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    private static readonly EntProtoId SmokeEffectPrototype = "ExplosionEffectSmokeOpaque";

    private void SpawnSunriseExplosionSmoke(
        int iterationCount,
        MapCoordinates epicenter)
    {
        var smokeCount = iterationCount / 2;
        for (var i = 0; i < smokeCount; i++)
        {
            var angle = _robustRandom.NextDouble() * Math.Tau;
            var distance = _robustRandom.NextDouble() * iterationCount;
            var offset = new Vector2(
                (float) (Math.Cos(angle) * distance),
                (float) (Math.Sin(angle) * distance));

            var smokeMapCoordinates = new MapCoordinates(epicenter.Position + offset, epicenter.MapId);
            if (!_mapManager.TryFindGridAt(smokeMapCoordinates, out var gridUid, out var grid))
                continue;

            // Сохраняем дым в локальных координатах повёрнутой сетки и центрируем на клетке.
            var smokeCoordinates = _map.MapToGrid(gridUid, smokeMapCoordinates).SnapToGrid(grid);
            Spawn(SmokeEffectPrototype, smokeCoordinates);
        }
    }
}
