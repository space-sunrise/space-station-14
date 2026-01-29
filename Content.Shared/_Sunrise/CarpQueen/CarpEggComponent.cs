using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CarpQueen;

[RegisterComponent, NetworkedComponent]
public sealed partial class CarpEggComponent : Component
{
    [DataField("queen")] public EntityUid? Queen;

    /// <summary>
    /// Объем лужи, необходимый для вылупления.
    /// </summary>
    [DataField("requiredVolume")] public float RequiredVolume = 10f;

    /// <summary>
    /// Секунд между проверками вылупления.
    /// </summary>
    [DataField("checkInterval")] public float CheckInterval = 3f;

    [DataField("accum")] public float Accum;

    /// <summary>
    /// Секунд, сколько икра должна пролежать на жидкости перед вылуплением.
    /// </summary>
    [DataField("hatchDelay")] public float HatchDelay = 5f;

    /// <summary>
    /// Достаточны ли текущие условия тайла для вылупления.
    /// </summary>
    [DataField("eligible")] public bool Eligible;

    /// <summary>
    /// Накопленное время ожидания без жидкости. При превышении MaxWaitWithoutLiquid икра ломается.
    /// </summary>
    [DataField("waitElapsed")] public float WaitElapsed;

    /// <summary>
    /// Максимум секунд ожидания появления жидкости перед разрушением икры.
    /// </summary>
    [DataField("maxWaitWithoutLiquid")] public float MaxWaitWithoutLiquid = 30f;

    /// <summary>
    /// Радиус (тайлы) поиска королевы при вылуплении.
    /// Если королева в радиусе - карп становится слугой, иначе запоминает игроков.
    /// </summary>
    [DataField("queenSearchRange")] public float QueenSearchRange = 3f;

    /// <summary>
    /// Радиус (тайлы) поиска игроков для запоминания, когда королевы нет рядом.
    /// </summary>
    [DataField("friendSearchRange")] public float FriendSearchRange = 6f;

    /// <summary>
    /// Количество реагента, впрыскиваемое при укусе карпа.
    /// </summary>
    [DataField("biteReagentAmount")] public FixedPoint2 BiteReagentAmount = FixedPoint2.New(1);
}


