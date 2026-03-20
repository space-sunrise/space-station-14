// Developed by Nox for the Sunrise Station project
// Author: KloopRe

using System.Reflection;
using System.Linq;
using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.TimeWindow;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared._Sunrise.Disease;
using Content.Shared.Whitelist;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Tag;
using Content.Shared.Body.Components;
using Content.Shared.Mobs;
using Content.Server._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared._Sunrise.Disease.Symptoms;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed partial class DiseaseSystem : SharedDiseaseSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    private DiseaseSymptomFactoryRegistry _symptomFactories = default!;
    private ISawmill _sawmill = default!;

    /// <summary>
    ///     Метка для сущностей, которые инфецируются со 100% вероятностью.
    /// </summary>
    public readonly ProtoId<TagPrototype> DiseaseAlwaysInfectableTag = "DiseaseAlwaysInfectable";

    /// <summary>
    ///     Метка для сущностей, которые игнорируют проверку возможности заражения.
    /// </summary>
    public readonly ProtoId<TagPrototype> IgnoreCanInfectTag = "IgnoreCanInfect";

    /// <summary>
    ///     Во время EntityQueryEnumerator может произойти изменение query из-за обновления симптома.
    ///     Поэтому требуется обновлять в списке.
    /// </summary>
    private readonly List<EntityUid> _diseaseUpdateQueue = new();
    public const SlotFlags ProtectiveSlots =
            SlotFlags.FEET |
            SlotFlags.HEAD |
            SlotFlags.EYES |
            SlotFlags.GLOVES |
            SlotFlags.MASK |
            SlotFlags.NECK |
            SlotFlags.INNERCLOTHING |
            SlotFlags.OUTERCLOTHING;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("DiseaseSystem");

        _symptomFactories = new DiseaseSymptomFactoryRegistry();
        AutoRegisterSymptoms();

        SubscribeLocalEvent<DiseaseComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DiseaseComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DiseaseComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DiseaseComponent, CauseDiseaseEvent>(OnCauseDisease);
        SubscribeLocalEvent<DiseaseComponent, CureDiseaseEvent>(OnCureDisease);
        SubscribeLocalEvent<DiseaseComponent, EntityZombifiedEvent>(OnEntityZombified);

        AddSpeedInitialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.DiseaseUpdateWindow != null &&
                _timedWindowSystem.IsExpired(component.DiseaseUpdateWindow))
            {
                _timedWindowSystem.Reset(component.DiseaseUpdateWindow);
                _diseaseUpdateQueue.Add(uid);
            }
        }

        foreach (var uid in _diseaseUpdateQueue)
        {
            if (!TryComp<DiseaseComponent>(uid, out var component))
                continue;

            UpdateDisease(uid, component);
        }

        _diseaseUpdateQueue.Clear();
    }

    private void OnEntityZombified(Entity<DiseaseComponent> entity, ref EntityZombifiedEvent args)
    {
        CureDisease(entity);
    }

    private void OnCauseDisease(Entity<DiseaseComponent> entity, ref CauseDiseaseEvent args)
    {
        RebuildSymptoms(entity, args.SourceData);
    }

    private void OnCureDisease(Entity<DiseaseComponent> entity, ref CureDiseaseEvent args)
    {
        if (_mobState.IsDead(entity))
            return;

        // При изличении вырабатывается иммунитет
        var immun = EnsureComp<DiseaseImmunComponent>(entity);
        immun.StrainsId.Add(entity.Comp.Data.StrainId);
    }

    private void OnMobStateChanged(EntityUid uid, DiseaseComponent component, MobStateChangedEvent args)
    {
        component.PatientState = args.NewMobState;
    }

    private void UpdateDisease(EntityUid uid, DiseaseComponent component)
    {
        component.Data.MutationPoints += component.Data.RegenMutationPoints;

        if (CanManifestInHost((uid, component)))
        {
            foreach (var symptom in component.ActiveSymptomInstances)
            {
                symptom.OnUpdate(uid, component);
            }
        }

        if (!BaseDiseaseSettings.DebuffDiseaseMultipliers.TryGetValue(component.RegenerationType, out var regenMultiplier))
            regenMultiplier = 1.0f;

        var totalPoints = component.Data.RegenThreshold * regenMultiplier;

        if (component.PatientState is MobState.Dead)
            totalPoints = -component.Data.DamageWhenDead;

        AddThresholdPoints((uid, component), totalPoints);
    }

    private void OnComponentInit(EntityUid uid, DiseaseComponent component, ComponentInit args)
    {
        _timedWindowSystem.Reset(component.DiseaseUpdateWindow);

        RefreshSymptoms((uid, component));
    }

    private void OnShutdown(EntityUid uid, DiseaseComponent component, ComponentShutdown args)
    {
        foreach (var symptom in component.ActiveSymptomInstances)
        {
            symptom.OnRemoved(uid, component);
        }
    }

    /// <summary>
    ///     Изменяет здоровье вируса.
    /// </summary>
    public DiseaseData CreateNewDisease(string strainId = "")
    {
        DiseaseData data = new DiseaseData(strainId);

        var whitelist = new EntityWhitelist();
        whitelist.Components = BaseDiseaseSettings.DefaultWhitelistComponents.ToArray();

        data.EntityWhitelist = whitelist;
        return data;
    }

    /// <summary>
    ///     Изменяет здоровье вируса.
    /// </summary>
    public void AddThresholdPoints(Entity<DiseaseComponent?> host, float points = 1f)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        if (host.Comp.Data.Threshold + points >= host.Comp.Data.MaxThreshold)
            return;

        host.Comp.Data.Threshold += points;

        if (host.Comp.Data.Threshold <= 0)
            CureDisease(host, host.Comp);
    }

    /// <summary>
    ///     Инфецируемый распространяет инфекцию вокруг себя.
    /// </summary>
    public void InfectAround(Entity<DiseaseComponent?> host, float range = 1f)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        InfectAround(host, range, host.Comp);
    }

    /// <summary>
    ///     Обновляет DiseaseData по логике интерфейсов симптомов в компонент.
    /// </summary>
    public void RefreshSymptoms(Entity<DiseaseComponent?> host)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        // Собираем активные типы симптомов из данных вируса
        var activeTypes = new HashSet<ProtoId<DiseaseSymptomPrototype>>();
        if (host.Comp.Data.ActiveSymptom != null)
        {
            foreach (var protoSymptom in host.Comp.Data.ActiveSymptom)
            {
                activeTypes.Add(protoSymptom);
            }
        }

        // Удаляем симптомы, которых больше нет в ActiveSymptom
        for (var i = host.Comp.ActiveSymptomInstances.Count - 1; i >= 0; i--)
        {
            var instance = host.Comp.ActiveSymptomInstances[i];
            if (!activeTypes.Contains(instance.PrototypeId))
            {
                if (CanManifestInHost((host, host.Comp)))
                    instance.OnRemoved(host, host.Comp);
                else
                    instance.ApplyDataEffect(host.Comp.Data, false);
                host.Comp.ActiveSymptomInstances.RemoveAt(i);
            }
        }

        // Добавляем новые симптомы
        if (host.Comp.Data.ActiveSymptom != null)
        {
            foreach (var protoSymptom in host.Comp.Data.ActiveSymptom)
            {
                if (!_prototype.TryIndex(protoSymptom, out var prototype))
                    continue;

                if (host.Comp.ActiveSymptomInstances.Any(s => s.PrototypeId == prototype.ID))
                    continue;

                var symptomInstance = CreateSymptomInstance(protoSymptom);
                host.Comp.ActiveSymptomInstances.Add(symptomInstance);

                if (CanManifestInHost((host, host.Comp)))
                {
                    _sawmill.Debug($"Добавлен ActiveSymptomInstance {symptomInstance.ToString()} к сущности {host.Owner}.");
                    symptomInstance.OnAdded(host, host.Comp);
                }
                else
                {
                    symptomInstance.ApplyDataEffect(host.Comp.Data, true);
                }
            }
        }
    }

    /// <summary>
    ///     Используйте RebuildSymptoms, а не RefreshSymptoms, если данные должны соответствовать источнику
    ///     Добавляет интерфейсы в компонент из симптомов DiseaseData.
    ///     Полностью сносим и пересобираем под DiseaseData из источника, иная логика может привести к ошибкам.
    /// </summary>
    public void RebuildSymptoms(Entity<DiseaseComponent> host, DiseaseData source)
    {
        for (var i = host.Comp.ActiveSymptomInstances.Count - 1; i >= 0; i--)
        {
            var instance = host.Comp.ActiveSymptomInstances[i];

            if (CanManifestInHost((host, host.Comp)))
                instance.OnRemoved(host, host.Comp);
            else
                instance.ApplyDataEffect(host.Comp.Data, false);

            host.Comp.ActiveSymptomInstances.RemoveAt(i);
        }

        host.Comp.Data = (DiseaseData)source.CloneForInfection();

        foreach (var protoSymptom in host.Comp.Data.ActiveSymptom)
        {
            var instance = CreateSymptomInstance(protoSymptom);
            host.Comp.ActiveSymptomInstances.Add(instance);

            if (CanManifestInHost((host, host.Comp)))
            {
                _sawmill.Debug(
                    $"Добавлен ActiveSymptomInstance {instance} к сущности {host.Owner}");
                instance.OnAdded(host, host.Comp);
            }
            else
            {
                instance.ApplyDataEffect(host.Comp.Data, true);
            }
        }
    }

    public void InfectAround(EntityUid host, float range = 1f, DiseaseComponent? component = null)
    {
        if (!Resolve(host, ref component, false))
            return;

        // Берём только мобов
        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(host, Transform(host)), range).ToList();

        if (entities.Count <= 0)
            return;

        foreach (var ent in entities)
        {
            var target = ent.Owner;

            if (target == host)
                continue;

            if (!_interaction.InRangeUnobstructed(host, target, range, CollisionGroup.Opaque))
                continue;

            ProbInfect((host, component), target);
        }
    }

    /// <summary>
    ///     Заразить с вероятностью.
    /// </summary>
    public void ProbInfect(Entity<DiseaseComponent?> host, EntityUid target, bool ignoreResistance = false, float? infectivity = null)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        ProbInfect(host.Comp.Data, target, host, ignoreResistance, infectivity);
    }

    /// <summary>
    ///     Если у сущности есть DiseaseComponent, то не используем CalcInfectionInfectivity, т.к. он уже расчитан для этого случая.
    /// </summary>
    public void ProbInfect(DiseaseData data, EntityUid target, EntityUid? host = null, bool ignoreResistance = false, float? infectivity = null)
    {
        var ev = new ProbInfectAttemptEvent(target, false, host);
        RaiseLocalEvent(target, ev);

        if (ev.Cancel)
            return;

        if (!CanInfect(target, data) && !_tag.HasTag(target, IgnoreCanInfectTag))
            return;

        if (_tag.HasTag(target, DiseaseAlwaysInfectableTag))
        {
            InfectEntity(data, target);
            return;
        }

        // Вычисляем шанс заражения
        var finalChance = infectivity ?? data.Infectivity;

        if (!ignoreResistance)
            finalChance = GetDiseaseInfectionChance(target, data, infectivity);

        // Бросаем шанс
        if (_random.Prob(finalChance))
        {
            _sawmill.Debug($"[{host}] заразил [{target}] вирусом {data.StrainId} (шанс {finalChance:P0})");
            InfectEntity(data, target);
        }
        else
        {
            _sawmill.Debug($"[{host}] не заразил [{target}] (шанс {finalChance:P0})");
        }
    }

    public void InfectEntity(Entity<DiseaseComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp, false))
            return;

        InfectEntity(source.Comp.Data, target);
    }

    public void InfectEntity(DiseaseData data, EntityUid target)
    {
        if (TryComp<DiseaseComponent>(target, out var targetDisease)
            && targetDisease.Data.StrainId == data.StrainId)
        {
            MergeMedicineResistance(data, targetDisease.Data);
        }

        // Проверяем PrimaryPatient и другой штамм
        if (TryComp<PrimaryPatientComponent>(target, out var patientComponent)
            && patientComponent.StrainId != data.StrainId)
        {
            RemComp<PrimaryPatientComponent>(target);
        }

        // В любом случае копируем остальные данные (например, симптомы, тела и т.п.)
        EnsureComp<DiseaseComponent>(target);

        var ev = new CauseDiseaseEvent(data);
        RaiseLocalEvent(target, ev);
    }

    private void MergeMedicineResistance(DiseaseData source, DiseaseData target)
    {
        foreach (var kvp in source.MedicineResistance)
        {
            if (target.MedicineResistance.TryGetValue(kvp.Key, out var existingValue))
            {
                // Берём лучший (максимальный) коэффициент
                target.MedicineResistance[kvp.Key] = Math.Max(existingValue, kvp.Value);
            }
            else
            {
                // Если элемента нет — добавляем
                target.MedicineResistance[kvp.Key] = kvp.Value;
            }
        }

        // Также переносим недостающие элементы из target в source, если нужно
        foreach (var kvp in target.MedicineResistance)
        {
            if (!source.MedicineResistance.ContainsKey(kvp.Key))
                source.MedicineResistance[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    ///     Возможность заразиться вирусом.
    /// </summary>
    public bool CanInfect(EntityUid target, DiseaseComponent component)
    {
        return CanInfect(target, component.Data);
    }

    public bool CanInfect(EntityUid target, DiseaseData data)
    {
        if (HasComp<ZombieComponent>(target)
            || HasComp<PendingZombieComponent>(target))
            return false;

        if (_mobState.IsDead(target))
            return false;

        if (TryComp<DiseaseImmunComponent>(target, out var immun) &&
            (immun.StrainsId.Contains(data.StrainId) || immun.ImmunAll))
        {
            return false;
        }

        if (TryComp<DiseaseComponent>(target, out var targetDiseaseComp))
        {
            // Сила вируса определяется по количеству симптомов            
            if (!ThisDiseasIsStronger(data, targetDiseaseComp.Data))
                return false;
        }

        if (!_whitelist.IsWhitelistPass(data.EntityWhitelist, target))
            return false;

        // Должно быть тело!
        if (TryComp<BodyComponent>(target, out var body)
            && body.Prototype != null
            && !data.BodyWhitelist.Contains(body.Prototype.Value))
            return false;

        return true;
    }

    /// <summary>
    ///     Если левая болезнь сильнее правой, возвращает true.
    /// </summary>
    public bool ThisDiseasIsStronger(DiseaseData left, DiseaseData right)
    {
        if (left.ActiveSymptom.Count > right.ActiveSymptom.Count)
            return true;

        return false;
    }

    public DiseaseData GenerateDiseaseData(
    Dictionary<DangerIndicatorSymptom, int> symptomsByDanger,
    int bodyCount,
    string strainId = "")
    {
        var data = CreateNewDisease(strainId);

        foreach (var (danger, count) in symptomsByDanger)
        {
            if (count <= 0)
                continue;

            var availableSymptoms = _prototype
                .EnumeratePrototypes<DiseaseSymptomPrototype>()
                .Where(p =>
                    p.DangerIndicator == danger &&
                    p.CanGenerateRandomly &&
                    !data.ActiveSymptom.Contains(p.ID))
                .ToList();

            if (availableSymptoms.Count == 0)
                continue;

            var toAdd = Math.Min(count, availableSymptoms.Count);

            for (var i = 0; i < toAdd; i++)
            {
                var picked = _random.PickAndTake(availableSymptoms);
                data.ActiveSymptom.Add(picked.ID);

                if (availableSymptoms.Count == 0)
                    break;
            }
        }

        if (bodyCount > 0)
        {
            var availableBodies = _prototype
                .EnumeratePrototypes<BodyPrototype>()
                .Select(p => p.ID)
                .Where(id => !BaseDiseaseSettings.BodyBlackList.Contains(id) && !data.BodyWhitelist.Contains(id))
                .ToList();

            if (availableBodies.Count > 0)
            {
                var toAdd = Math.Min(bodyCount, availableBodies.Count);

                for (var i = 0; i < toAdd; i++)
                {
                    var body = _random.PickAndTake(availableBodies);
                    data.BodyWhitelist.Add(body);

                    if (availableBodies.Count == 0)
                        break;
                }
            }
        }

        return data;
    }

    public void CureDisease(EntityUid uid, DiseaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        RaiseLocalEvent(uid, new CureDiseaseEvent(uid));

        RemComp<DiseaseComponent>(uid);
    }

    public float CalcInfectionInfectivity(EntityUid target, DiseaseComponent? component = null)
    {
        if (!Resolve(target, ref component))
            return 0f;

        return CalcInfectionInfectivity(component.Data);
    }

    public float CalcInfectionInfectivity(DiseaseData data)
    {
        var infectivity = data.Infectivity;

        foreach (var symptomId in data.ActiveSymptom)
        {
            if (_prototype.TryIndex(symptomId, out var proto))
                infectivity += proto.AddInfectivity;
        }

        return infectivity;
    }

    /// <summary>
    ///     Пытается нанести урон вирусу антибиотиком.
    ///     С каждым применением увеличивает сопротивление к этому антибиотику.
    ///     Сопротивление никогда не опускается ниже DefaultMedicineResistance.
    /// </summary>
    public void ApplyMedicineDamage(
        Entity<DiseaseComponent?> target,
        string medicine,
        float baseDamage,
        float resistanceIncrease = 0.05f,
        float maxResistance = 0.9f)
    {
        if (!Resolve(target, ref target.Comp, false))
            return;

        var data = target.Comp.Data;

        // Получаем текущее сопротивление (не ниже дефолтного)
        if (!data.MedicineResistance.TryGetValue(medicine, out var resistance))
        {
            resistance = data.DefaultMedicineResistance;
        }
        else
        {
            resistance = Math.Max(resistance, data.DefaultMedicineResistance);
        }

        // Считаем фактический урон
        var damageMultiplier = Math.Clamp(1f - resistance, 0f, 1f);
        var finalDamage = baseDamage * damageMultiplier;

        if (finalDamage > 0f)
            AddThresholdPoints(target, -finalDamage);

        // Увеличиваем сопротивление
        var newResistance = resistance + resistanceIncrease;

        newResistance = Math.Clamp(
            newResistance,
            data.DefaultMedicineResistance,
            maxResistance
        );

        data.MedicineResistance[medicine] = newResistance;
    }

    private float GetDiseaseInfectionChance(Entity<DiseaseComponent?> entity, float? infectivity = null)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return 0f;

        return GetDiseaseInfectionChance(entity.Owner, entity.Comp.Data, infectivity);
    }

    private float GetDiseaseInfectionChance(EntityUid target, DiseaseData data, float? infectivity = null)
    {
        var resistanceQuery = new DiseaseResistanceQueryEvent(ProtectiveSlots);
        RaiseLocalEvent(target, resistanceQuery);

        var finalChance = (infectivity ?? data.Infectivity) - resistanceQuery.TotalCoefficient;

        // от 0 до 100%
        finalChance = Math.Clamp(finalChance, 0f, 1.0f);

        return finalChance;
    }

    public IDiseaseSymptom CreateSymptomInstance(ProtoId<DiseaseSymptomPrototype> symptomId)
    {
        if (!_prototype.TryIndex(symptomId, out var proto))
            throw new Exception($"No prototype for symptom {symptomId}");

        var window = proto.IntervalWindow != null ? proto.IntervalWindow.Clone() : DefaultSymptomWindow.Clone();

        return _symptomFactories.Create(symptomId, window);
    }


    public bool TryGetSymptom<T>(Entity<DiseaseComponent?> entity, out T? symptom)
    where T : class, IDiseaseSymptom
    {
        symptom = null;

        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента DiseaseComponent, невозможно получить симптом {typeof(T).Name}.");
            return false;
        }

        symptom = entity.Comp.ActiveSymptomInstances.OfType<T>().FirstOrDefault();
        return symptom != null;
    }

    public T EnsureSymptom<T>(Entity<DiseaseComponent?> entity)
    where T : IDiseaseSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента DiseaseComponent, невозможно добавить симптом {typeof(T).Name}.");
            return default!;
        }

        // Ищем симптом нужного типа
        var existing = entity.Comp.ActiveSymptomInstances.OfType<T>().FirstOrDefault();
        if (existing != null)
            return existing;

        return AddSymptom<T>(entity);
    }

    public T AddSymptom<T>(Entity<DiseaseComponent?> entity)
    where T : IDiseaseSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента DiseaseComponent, невозможно добавить симптом {typeof(T).Name}.");
            return default!;
        }

        var attr = typeof(T).GetCustomAttribute<DiseaseSymptomAttribute>();
        if (attr == null)
        {
            _sawmill.Warning($"Symptom {typeof(T).Name} не имеет атрибута DiseaseSymptomAttribute, невозможно добавить симптом.");
            return default!;
        }

        ProtoId<DiseaseSymptomPrototype> protoId;
        try
        {
            protoId = attr.Id;
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Symptom {typeof(T).Name} имеет некорректный protoId '{attr.Id}': {e}.");
            return default!;
        }

        var existing = entity.Comp.ActiveSymptomInstances.FirstOrDefault(s => s.PrototypeId == protoId);
        if (existing is T existingTyped)
            return existingTyped;

        if (existing != null)
        {
            _sawmill.Warning(
                $"У сущности {entity.Owner} уже есть симптом с protoId '{protoId}', но его тип {existing.GetType().Name} не совпадает с ожидаемым {typeof(T).Name}."
            );
            return default!;
        }

        IDiseaseSymptom created;
        try
        {
            created = CreateSymptomInstance(protoId);
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Не удалось создать симптом {typeof(T).Name} (protoId '{protoId}') для сущности {entity.Owner}: {e}.");
            return default!;
        }

        if (created is not T symptom)
        {
            _sawmill.Warning(
                $"Фабрика симптомов вернула {created.GetType().Name} вместо {typeof(T).Name} (protoId '{protoId}').");
            return default!;
        }

        entity.Comp.ActiveSymptomInstances.Add(symptom);

        if (CanManifestInHost((entity.Owner, entity.Comp)))
            symptom.OnAdded(entity.Owner, entity.Comp);
        else
            symptom.ApplyDataEffect(entity.Comp.Data, true);

        _sawmill.Debug($"Добавлен симптом {typeof(T).Name} к сущности {entity.Owner}.");

        return symptom;
    }

    public void RemoveSymptom<T>(Entity<DiseaseComponent?> entity)
    where T : IDiseaseSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента DiseaseComponent, невозможно удалить симптом {typeof(T).Name}.");
            return;
        }

        if (entity.Comp.ActiveSymptomInstances == null)
            return;

        var symptom = entity.Comp.ActiveSymptomInstances.FirstOrDefault(s => s is T);
        if (symptom == null)
            return;

        if (CanManifestInHost((entity.Owner, entity.Comp)))
            symptom.OnRemoved(entity.Owner, entity.Comp);
        else
            symptom.ApplyDataEffect(entity.Comp.Data, false);

        entity.Comp.ActiveSymptomInstances.Remove(symptom);

        _sawmill.Debug($"Удалён симптом {typeof(T).Name} у сущности {entity.Owner}.");
    }

    private void AutoRegisterSymptoms()
    {
        _sawmill.Info("[Disease] Scanning assemblies for symptoms...");

        var count = 0;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!typeof(IDiseaseSymptom).IsAssignableFrom(type))
                    continue;

                var attr = type.GetCustomAttribute<DiseaseSymptomAttribute>();
                if (attr == null)
                    continue;

                ProtoId<DiseaseSymptomPrototype> protoId;

                try
                {
                    protoId = attr.Id;
                }
                catch (Exception e)
                {
                    _sawmill.Error($"[Disease] Invalid protoId '{attr.Id}' in {type.FullName}: {e}");
                    continue;
                }

                // Проверка существования прототипа, чтобы не регистрировать фабрику для несуществующих симптомов
                if (!_prototype.HasIndex(protoId))
                {
                    _sawmill.Error($"[Disease] Prototype '{protoId}' NOT FOUND for {type.FullName}");
                    continue;
                }

                if (_symptomFactories.Contains(protoId))
                {
                    _sawmill.Error($"[Disease] DUPLICATE protoId '{protoId}' in {type.FullName}");
                    continue;
                }

                _sawmill.Info($"[Disease] REGISTER {type.Name} => {protoId} ({asm.GetName().Name})");

                _symptomFactories.Register(protoId, type);
                count++;
            }
        }

        _sawmill.Info($"[Disease] Registered {count} symptoms.");
    }

}
