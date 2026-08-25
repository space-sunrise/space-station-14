using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DantalionComponent : VampireClassComponent
{
	/// <summary>
	///     Базовый лимит тхраллов до учёта бонусов за кровь/силу
	/// </summary>
	[DataField]
	public int BaseThrallLimit = 1;

	/// <summary>
	///     Отслеживание порабощённых сущностей во время игры
	/// </summary>
	[ViewVariables(VVAccess.ReadOnly)]
	public HashSet<EntityUid> Thralls = new();

	/// <summary>
	///     Занятые слоты тхраллов. Не уменьшаются при потере тхраллов.
	/// </summary>
	[ViewVariables(VVAccess.ReadOnly)]
	public int ThrallSlotsUsed = 0;

	/// <summary>
	///     Активна ли сейчас Кровавая связь
	/// </summary>
	[ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
	public bool BloodBondActive = false;

	/// <summary>
	///     Идентификатор цикла Кровавой связи против дублирующих циклов
	/// </summary>
	public int BloodBondLoopId = 0;

	/// <summary>
	///     Тхраллы, связанные Кровавой связью
	/// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
	public List<EntityUid> BloodBondLinkedThralls = new();

    [DataField, AutoNetworkedField]
    public EntProtoId BloodBondBeamPrototype = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool BloodBondProcessingDamage = false;

    [DataField]
    public EntProtoId RallyOverlayEffect = "VampireRallyOverlayEffect";

    [DataField]
    public int ThrallHealBurn = 3;

    [DataField]
    public int ThrallHealBrute = 3;

    [DataField]
    public int ThrallHealAsphyxiation = 5;

    [DataField]
    public int ThrallLevel2Blood = 400;

    [DataField]
    public int ThrallLevel3Blood = 600;

    [DataField]
    public int HealBloodThreshold = 300;

    [DataField]
    public Dictionary<string, int> ThrallHealGroups = new()
    {
        { "Brute", 3 },
        { "Burn", 3 },
    };

    [DataField]
    public Dictionary<string, int> ThrallHealTypes = new()
    {
        { "Asphyxiation", 5 },
    };
}
