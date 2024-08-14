// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Store;
using Content.Shared.Chemistry.Reagent;
namespace Content.Shared._Sunrise.Disease;

[RegisterComponent]
public sealed partial class DiseaseRoleComponent : Component
{
    [DataField("actions", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, EntityPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, int> Actions = new();

    [DataField("infected")]
    public List<EntityUid> Infected = new();

    [DataField] public EntityUid? Action;

    [DataField] public Dictionary<string, (int, int)> Symptoms = new();

    [DataField] public int FreeInfects = 3;
    [DataField] public int InfectCost = 10;

    [DataField("currencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string CurrencyPrototype = "DiseasePoints";


    [DataField] public float BaseInfectChance = 0.6f;
    [DataField] public float CoughInfectChance = 0.2f;

    [DataField] public int Lethal = 0;
    [DataField] public int Shield = 1;

    [DataField] public int SickOfAllTime = 0;

    [DataField("newBloodReagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string NewBloodReagent = "ZombieBlood";
}
