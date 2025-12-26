using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.Helpers;

public static class EntityCoordinatesExtensions
{
    /// <summary>
    /// Генерирует случайные координаты в заданном радиусе от исходных координат.
    /// </summary>
    /// <param name="origin">Исходные координаты.</param>
    /// <param name="radius">Радиус для генерации случайной точки.</param>
    /// <param name="rand">Опциональный экземпляр Random для контроля генерации.</param>
    /// <returns>Новые координаты в пределах радиуса.</returns>
    public static EntityCoordinates GetRandomInRadius(this EntityCoordinates origin, float radius, IRobustRandom? rand = null)
    {
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");

        if (radius == 0)
            return origin;

        rand ??= IoCManager.Resolve<IRobustRandom>();

        // Генерируем угол и расстояние.
        var angle = rand.NextDouble() * Math.Tau; // 0..2π
        var distance = Math.Sqrt(rand.NextDouble()) * radius; // Равномерное распределение

        // Преобразуем в декартовы координаты.
        var x = (float)(distance * Math.Cos(angle));
        var y = (float)(distance * Math.Sin(angle));

        // Смещаем относительно исходной позиции.
        var newPosition = origin.Position + new Vector2(x, y);

        return new EntityCoordinates(origin.EntityId, newPosition);
    }
}
