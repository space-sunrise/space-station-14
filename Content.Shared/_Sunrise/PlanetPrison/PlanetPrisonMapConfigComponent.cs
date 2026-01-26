using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.PlanetPrison;

[RegisterComponent]
public sealed partial class PlanetPrisonMapConfigComponent : Component
{
    /// <summary>
    /// Минимальное количество игроков, необходимое для запуска этой карты тюрьмы
    /// </summary>
    [DataField]
    public int MinPlayersRequired = 2;
}