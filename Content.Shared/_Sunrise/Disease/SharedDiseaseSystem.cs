// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Disease.Prototypes;
using Content.Shared.Body.Prototypes;
using System.Linq;
using Content.Shared._Sunrise.TimeWindow;
using Content.Shared.Zombies;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Content.Shared._Sunrise.Disease.Symptoms;

namespace Content.Shared._Sunrise.Disease;

public enum BedRegenerationType
{
    None = 0,      // Не влияет
    Normal,        // Обычная кровать
    Stasis         // Стазис-кровать
}

public struct BaseDiseaseSettings
{
    /// <summary>
    ///     Бит видимости для отображения заражённых облаков (DiseaseInfectionCloud).
    /// </summary>
    public const int DiseaseInfectionVisibilityFlag = 1 << 4;

    /// <summary>
    ///     Стандартная цена для удаления тела.
    /// </summary>
    public const int StaticPriceDeleteBody = 1000;

    /// <summary>
    ///     Стандартная цена для удаления симптома.
    /// </summary>
    public const int StaticPriceDeleteSymptom = 1000;

    /// <summary>
    ///     Стандартная цена для мутации всех тел.
    /// </summary>
    public const int StaticBodyPrice = 500;

    /// <summary>
    ///     Стандартный для всех вирусов белый список компонентов.
    /// </summary>
    public static readonly string[] DefaultWhitelistComponents =
    {
        "MobState",
        "Bloodstream"
    };

    /// <summary>
    ///     Список не доступных для покупки тел.
    /// </summary>
    public static readonly List<ProtoId<BodyPrototype>> BodyBlackList = new List<ProtoId<BodyPrototype>>
    {
        "Abductor",
        "ConstructJuggernaut",
        "ManeaterAnimal",
        "Chiken",
        "SVBodyDoubleClown",
        "Milira",
        "Predator",
        "Resomi",
        "Reaper",
        "Swine",
        "TerminatorFlesh",
        "TerminatorEndoskeleton",
        "Bot",
        "HugBot",
        "Gingerbread",
        "Skeleton",
        "Rat",
        "Mouse",
        "Bloodsucker",
        "AnimalHemocyanin",
        "AnimalNymphBrain",
        "AnimalNymphLungs",
        "AnimalNymphStomach",
        "AnimalRuminant",
        "Slimes",
        "Mothroach",
        "SmartCorgi"
    };

    /// <summary>
    ///     Модификаторы ослабления вируса в зависимости от состояния.
    /// </summary>
    public static readonly Dictionary<BedRegenerationType, float> DebuffDiseaseMultipliers =
        new()
        {
            { BedRegenerationType.None, 1.0f },
            { BedRegenerationType.Normal, 0.5f },
            { BedRegenerationType.Stasis, 0.1f },
        };
}

public abstract partial class SharedDiseaseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    private ISawmill _sawmill = default!;

    /// <summary>
    ///     Стандартное окно времени проявления симптом.
    /// </summary>
    protected TimedWindow DefaultSymptomWindow = new TimedWindow(TimeSpan.FromSeconds(15f), TimeSpan.FromSeconds(60f));

    /// <summary>
    ///     Метка для сущностей, которые не могут проявить симпптомы.
    /// </summary>
    public readonly ProtoId<TagPrototype> DiseaseIgnorSymptomsTag = "DiseaseIgnorSymptoms";
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("SharedDiseaseSystem");
    }

    public int GetSymptomPrice(DiseaseData data, ProtoId<DiseaseSymptomPrototype> symptomId)
    {
        if (!_prototype.TryIndex(symptomId, out var proto))
            return 0;

        return Math.Max(1, data.ActiveSymptom.Count) * proto.Price;
    }

    public int GetSymptomPrice(List<ProtoId<DiseaseSymptomPrototype>> symptoms, ProtoId<DiseaseSymptomPrototype> symptomId)
    {
        if (!_prototype.TryIndex(symptomId, out var proto))
            return 0;

        return Math.Max(1, symptoms.Count) * proto.Price;
    }

    public int GetSymptomPrice(List<ProtoId<DiseaseSymptomPrototype>> symptoms, DiseaseSymptomPrototype proto)
    {
        return Math.Max(1, symptoms.Count) * proto.Price;
    }

    public int GetSymptomPrice(DiseaseData data, DiseaseSymptomPrototype proto)
    {
        return Math.Max(1, data.ActiveSymptom.Count) * proto.Price;
    }

    public int GetBodyPrice(DiseaseData data)
    {
        return Math.Max(1, data.BodyWhitelist.Count) * BaseDiseaseSettings.StaticBodyPrice;
    }

    public int GetBodyPrice(List<ProtoId<BodyPrototype>> bodyWhitelist)
    {
        return Math.Max(1, bodyWhitelist.Count) * BaseDiseaseSettings.StaticBodyPrice;
    }

    public int GetBodyDeletePrice()
    {
        return BaseDiseaseSettings.StaticPriceDeleteBody;
    }

    public int GetSymptomDeletePrice(int multiPriceDeleteSymptom)
    {
        return Math.Max(1, multiPriceDeleteSymptom) * BaseDiseaseSettings.StaticPriceDeleteSymptom;
    }

    public int GetQuantityInfected(string strainId)
    {
        int quantity = 0;

        var query = EntityQueryEnumerator<DiseaseComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (component.Data.StrainId == strainId)
                quantity++;
        }

        return quantity;
    }

    /// <summary>
    ///     Могут ли симптомы проявиться.
    /// </summary>
    public bool CanManifestInHost(Entity<DiseaseComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (_tag.HasTag(entity, DiseaseIgnorSymptomsTag))
            return false;

        if (_mobState.IsDead(entity))
            return false;

        if (HasComp<PrimaryPacientComponent>(entity)
            || HasComp<ZombieComponent>(entity))
            return false;

        return true;
    }

    public bool HasSymptom<T>(Entity<DiseaseComponent?> entity)
    where T : IDiseaseSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента DiseaseComponent, невозможно проверить наличие симптома {typeof(T).Name}.");
            return default!;
        }

        return entity.Comp.ActiveSymptomInstances.Any(s => s is T);
    }
}
