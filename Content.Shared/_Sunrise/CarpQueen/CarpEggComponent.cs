using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CarpQueen;

[RegisterComponent, NetworkedComponent]
public sealed partial class CarpEggComponent : Component
{
    [DataField("queen")] public EntityUid? Queen;

    /// <summary>
    /// Требуемый объем лужи в единицах для вылупления.
    /// </summary>
    [DataField("requiredVolume")] public float RequiredVolume = 15f;

    /// <summary>
    /// Секунды между проверками вылупления.
    /// </summary>
    [DataField("checkInterval")] public float CheckInterval = 3f;

    [DataField("accum")] public float Accum;

    /// <summary>
    /// Сколько секунд яйцо должно оставаться на подходящей жидкости перед вылуплением.
    /// </summary>
    [DataField("hatchDelay")] public float HatchDelay = 5f;

    /// <summary>
    /// Достаточны ли текущие условия тайла для вылупления.
    /// </summary>
    [DataField("eligible")] public bool Eligible;

    /// <summary>
    /// Накопленное время ожидания без подходящей жидкости. Если превышает MaxWaitWithoutLiquid, яйцо ломается.
    /// </summary>
    [DataField("waitElapsed")] public float WaitElapsed;

    /// <summary>
    /// Максимальное число секунд ожидания появления жидкости перед разрушением яйца.
    /// </summary>
    [DataField("maxWaitWithoutLiquid")] public float MaxWaitWithoutLiquid = 30f;

    /// <summary>
    /// Радиус в тайлах для проверки, находится ли королева рядом при вылуплении.
    /// Если королева в этом радиусе, карп становится слугой; иначе запоминает ближайших игроков.
    /// </summary>
    [DataField("queenCheckRange")] public float QueenCheckRange = 3f;

    /// <summary>
    /// Радиус в тайлах для поиска ближайших игроков, которых нужно запомнить, если королевы рядом нет.
    /// </summary>
    [DataField("friendSearchRange")] public float FriendSearchRange = 3f;
}

