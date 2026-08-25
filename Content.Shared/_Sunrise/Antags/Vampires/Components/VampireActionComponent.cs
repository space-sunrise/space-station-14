namespace Content.Shared._Sunrise.Antags.Vampires.Components;

using Content.Shared._Sunrise.Antags.Vampires.Prototypes;
using Robust.Shared.Prototypes;

/// <summary>
/// Навешивается на созданную сущность действия для задания вампирских условий и стоимостей
/// - BloodToUnlock — требуемый TotalBlood для открытия действия
/// - BloodCost — количество крови на использование
/// - RequiredClass — необязательное требование класса для действия
/// - RequiresFullPower — должна ли быть достигнута полная сила
/// - AllowNonVampireUsers — позволяет напрямую выданным админ/дебаг действиям работать без VampireComponent
/// </summary>
[RegisterComponent]
public sealed partial class VampireActionComponent : Component
{
    [DataField]
    public int BloodToUnlock = 0;

    [DataField]
    public float BloodCost = 0f;

    [DataField]
    public ProtoId<VampireClassPrototype>? RequiredClass = null;

    [DataField]
    public bool RequiresFullPower;

    [DataField]
    public bool AllowNonVampireUsers = true;
}
