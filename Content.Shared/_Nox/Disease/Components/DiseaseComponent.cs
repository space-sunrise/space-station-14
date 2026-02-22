// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

using Content.Shared.Whitelist;
using System.Linq;
using Robust.Shared.Serialization;
using Content.Shared._Nox.Disease.Prototypes;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Body.Prototypes;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Content.Shared._Nox.Disease.Symptoms;
using System.Threading;

namespace Content.Shared._Nox.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseComponent : Component
{
    /// <summary>
    ///     Состояние носителя инфекции.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public MobState PatientState = new();

    /// <summary>
    ///     Данные об вирусе.
    /// </summary>
    [DataField]
    public DiseaseData Data = new();

    /// <summary>
    ///     Список активных симптомов для этого инфицированного тела.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<IDiseaseSymptom> ActiveSymptomInstances = new();

    /// <summary>
    ///     Окно времени обновления вируса.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow DiseaseUpdateWindow = new TimedWindow(TimeSpan.FromSeconds(1f), TimeSpan.FromSeconds(1f));

    public DiseaseComponent(DiseaseData data)
    {
        Data = data;
    }

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "DiseaseFaction";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public BedRegenerationType RegenerationType = BedRegenerationType.None;
}


/// <summary>
///     Класс содержит данные об вирусе.
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class DiseaseData : ReagentData
{
    private static long _strainIdCounter;

    /// <summary>
    ///     ID штамма.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string StrainId = string.Empty;

    /// <summary>
    ///     Очки мутации.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int MutationPoints = 0;

    /// <summary>
    ///     Модификатор стоимости удаления симптома.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int MultiPriceDeleteSymptom = 1;

    /// <summary>
    ///     Урон вирусу, если организм носителя мёртв.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DamageWhenDead = 5;

    /// <summary>
    ///     Регенерация вируса.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float RegenThreshold = 1;

    /// <summary>
    ///     Регенерация очков мутации.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int RegenMutationPoints = 1;

    /// <summary>
    ///     Список симптомов которые должны быть при инициализации.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<ProtoId<DiseaseSymptomPrototype>> ActiveSymptom = new();

    /// <summary>
    ///     Живучесть вируса. Если <= 0, организм считается вылеченным.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Threshold = 100f;

    /// <summary>
    ///     Максимальное количествоочков живучести.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MaxThreshold = 100f;

    /// <summary>
    ///     Стандартное значение сопротивления медикаментам (антибиотикам).
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DefaultMedicineResistance = 0f;

    /// <summary>
    ///     Сопротивление медикаментам, модификатор урона.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, float> MedicineResistance = new();

    /// <summary>
    ///     Показатель заразности вируса от 0 до 1.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Infectivity = 0f;

    /// <summary>
    ///     Допустимые к заражению сущности.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityWhitelist = new();

    /// <summary>
    ///     Допустимые к заражению расы.
    /// </summary>
    [DataField]
    public List<ProtoId<BodyPrototype>> BodyWhitelist = new();

    public DiseaseData()
    {
        InitializeWhitelist();
        EnsureStrainId();
    }

    public DiseaseData(string strainId)
    {
        InitializeWhitelist();
        StrainId = strainId;
        EnsureStrainId();
    }

    private void EnsureStrainId()
    {
        if (string.IsNullOrEmpty(StrainId))
            StrainId = GenerateStrainId();
    }

    public static string GenerateStrainId()
    {
        const int length = 6;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const ulong radix = 36;
        const ulong modulus = 2_176_782_336; // 36^6

        var counter = (ulong)Interlocked.Increment(ref _strainIdCounter);
        var ticks = (ulong)DateTime.UtcNow.Ticks;

        // Не криптографическая уникальность: достаточно для идентификатора штамма в рамках рантайма.
        var mixed = ticks ^ (counter * 0x9E3779B97F4A7C15UL);
        var value = mixed % modulus;

        var id = new char[length];
        for (var i = length - 1; i >= 0; i--)
        {
            var rem = (int)(value % radix);
            id[i] = chars[rem];
            value /= radix;
        }

        return new string(id);
    }

    private void InitializeWhitelist()
    {
        EntityWhitelist ??= new EntityWhitelist();

        EntityWhitelist.Components = BaseDiseaseSettings.DefaultWhitelistComponents.ToArray();
        EntityWhitelist.RequireAll = true;
    }

    public override bool Equals(ReagentData? other)
    {
        if (other is not DiseaseData o)
            return false;

        if (StrainId != o.StrainId)
            return false;

        if (!MathHelper.CloseTo(MutationPoints, o.MutationPoints))
            return false;

        if (!MathHelper.CloseTo(MultiPriceDeleteSymptom, o.MultiPriceDeleteSymptom))
            return false;

        if (!MathHelper.CloseTo(DamageWhenDead, o.DamageWhenDead))
            return false;

        if (!MathHelper.CloseTo(RegenThreshold, o.RegenThreshold))
            return false;

        if (!MathHelper.CloseTo(Threshold, o.Threshold))
            return false;

        if (!MathHelper.CloseTo(MaxThreshold, o.MaxThreshold))
            return false;

        if (!MathHelper.CloseTo(RegenMutationPoints, o.RegenMutationPoints))
            return false;

        if (!MathHelper.CloseTo(DefaultMedicineResistance, o.DefaultMedicineResistance))
            return false;

        if (!MathHelper.CloseTo(Infectivity, o.Infectivity))
            return false;

        if (!BodyWhitelist.SequenceEqual(o.BodyWhitelist))
            return false;

        if (MedicineResistance.Count != o.MedicineResistance.Count ||
            MedicineResistance.Except(o.MedicineResistance).Any())
            return false;

        if (!ActiveSymptom.SequenceEqual(o.ActiveSymptom))
            return false;

        if (EntityWhitelist is null && o.EntityWhitelist is null)
            return true;

        if (EntityWhitelist is null || o.EntityWhitelist is null)
            return false;

        return EntityWhitelist.Equals(o.EntityWhitelist);
    }

    public override ReagentData Clone()
    {
        return new DiseaseData
        {
            StrainId = StrainId,
            MutationPoints = MutationPoints,
            MultiPriceDeleteSymptom = MultiPriceDeleteSymptom,
            DamageWhenDead = DamageWhenDead,
            RegenThreshold = RegenThreshold,
            Threshold = Threshold,
            DefaultMedicineResistance = DefaultMedicineResistance,
            Infectivity = Infectivity,

            ActiveSymptom = ActiveSymptom.ToList(),
            BodyWhitelist = BodyWhitelist.ToList(),

            MedicineResistance = MedicineResistance
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),

            EntityWhitelist = EntityWhitelist is null
                ? null
                : new EntityWhitelist
                {
                    Components = EntityWhitelist.Components?.ToArray(),
                    Sizes = EntityWhitelist.Sizes?.ToList(),
                    Tags = EntityWhitelist.Tags?.ToList(),
                    RequireAll = EntityWhitelist.RequireAll
                }
        };
    }

    /// <summary>
    ///     Использовать этот метод для заражения, иначе атрибуты будут стакаться при RefreshSymptoms.
    /// </summary>
    public ReagentData CloneForInfection()
    {
        return new DiseaseData
        {
            StrainId = StrainId,
            ActiveSymptom = ActiveSymptom.ToList(),
            BodyWhitelist = BodyWhitelist.ToList(),

            MedicineResistance = MedicineResistance
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),

            EntityWhitelist = EntityWhitelist is null
                ? null
                : new EntityWhitelist
                {
                    Components = EntityWhitelist.Components?.ToArray(),
                    Sizes = EntityWhitelist.Sizes?.ToList(),
                    Tags = EntityWhitelist.Tags?.ToList(),
                    RequireAll = EntityWhitelist.RequireAll
                }
        };
    }

    /// <summary>
    ///     Копирует симптомы, тела и EntityWhitelist из другого источника DiseaseData.
    /// </summary>
    public void ApplyInfectionData(DiseaseData source)
    {
        ActiveSymptom = source.ActiveSymptom.ToList();
        BodyWhitelist = source.BodyWhitelist.ToList();

        EntityWhitelist = source.EntityWhitelist is null
            ? null
            : new EntityWhitelist
            {
                Components = source.EntityWhitelist.Components?.ToArray(),
                Sizes = source.EntityWhitelist.Sizes?.ToList(),
                Tags = source.EntityWhitelist.Tags?.ToList(),
                RequireAll = source.EntityWhitelist.RequireAll
            };
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(StrainId);
        hash.Add(MutationPoints);
        hash.Add(MultiPriceDeleteSymptom);
        hash.Add(DamageWhenDead);
        hash.Add(RegenThreshold);
        hash.Add(Threshold);
        hash.Add(MaxThreshold);
        hash.Add(RegenMutationPoints);
        hash.Add(DefaultMedicineResistance);
        hash.Add(Infectivity);

        foreach (var kvp in MedicineResistance)
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        foreach (var s in BodyWhitelist)
            hash.Add(s);

        foreach (var symptom in ActiveSymptom)
            hash.Add(symptom);

        if (EntityWhitelist != null)
        {
            if (EntityWhitelist.Components != null)
                foreach (var c in EntityWhitelist.Components)
                    hash.Add(c);

            if (EntityWhitelist.Sizes != null)
                foreach (var s in EntityWhitelist.Sizes)
                    hash.Add(s);

            if (EntityWhitelist.Tags != null)
                foreach (var t in EntityWhitelist.Tags)
                    hash.Add(t);

            hash.Add(EntityWhitelist.RequireAll);
        }

        return hash.ToHashCode();
    }

}