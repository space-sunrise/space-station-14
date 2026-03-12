namespace Content.Server._Sunrise.Other.StationOnlyDirectSpawn;

/// <summary>
/// Компонент маркер, который обозначает, что на станции нельзя будет заспавниться путем случайного выбора из пула доступных станций.
/// Это нужно, чтобы помечать станции, на которые появляться должно быть можно ТОЛЬКО путем прямого спавна на ней
/// </summary>
[RegisterComponent]
public sealed partial class StationOnlyDirectSpawnComponent : Component
{

}
