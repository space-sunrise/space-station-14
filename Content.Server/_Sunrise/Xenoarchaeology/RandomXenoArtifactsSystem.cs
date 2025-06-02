using System.Linq;
using Content.Server._Sunrise.Helpers;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Body.Organ;
using Content.Shared.GameTicking;
using Content.Shared.Item;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Xenoarchaeology;

public sealed class RandomXenoArtifactsSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    /// <summary>
    /// Соотношение предметов к предметам-артефактам. В % процентах.
    /// Стандартное соотношение: <see cref="SunriseCCVars.ItemToArtifactRatio"/>
    /// </summary>
    private float _itemToArtifactRatio;
    private bool _enabled = SunriseCCVars.EnableRandomArtifacts.DefaultValue;

    private static readonly EntProtoId BaseParent = "BaseRandomItemXenoArtifactComponents";
    private static EntityPrototype? _baseParentPrototype;

    private EntityQuery<TransformComponent> _xform;
    private EntityQuery<OrganComponent> _organs;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(SunriseCCVars.EnableRandomArtifacts, OnCvarChanged, true);
        _configuration.OnValueChanged(SunriseCCVars.ItemToArtifactRatio, r => _itemToArtifactRatio = r, true);

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);

        _xform = GetEntityQuery<TransformComponent>();
        _organs = GetEntityQuery<OrganComponent>();

        _sawmill = Logger.GetSawmill("sunrise.random_artifacts");
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        if (!_enabled)
            return;

        DeleteOldArtifacts();
        CreateRandomArtifacts();
    }

    #region Creation

    /// <summary>
    /// Превращает часть предметов в артефакты
    /// Количество зависит от выставленного значения в <see cref="SunriseCCVars.ItemToArtifactRatio"/>
    /// </summary>
    public void CreateRandomArtifacts()
    {
        if (!_prototype.TryIndex(BaseParent, out _baseParentPrototype))
        {
            _sawmill.Error("Error while creating BaseParent for RandomXenoArtifactsSystem");

            return;
        }

        var items = _helpers.GetAll<ItemComponent>()
            .Where(e => IsAppropriate(e.Owner))
            .ToList();

        _random.Shuffle(items);

        var reducedItems = _helpers.GetPercentageOfHashSet(items, _itemToArtifactRatio);

        foreach (var item in reducedItems)
        {
            MakeArtifact(item);
        }

        _sawmill.Debug($"{reducedItems.Count()} made into artifacts");
    }

    /// <summary>
    /// Проверяет, подходит ли выбранный предмет для превращения в артефакт-предмет
    /// </summary>
    private bool IsAppropriate(Entity<TransformComponent?> ent)
    {
        if (!_xform.Resolve(ref ent))
            return false;

        var station = _station.GetOwningStation(ent, ent.Comp);

        if (!HasComp<StationRandomXenoArtifactComponent>(station))
            return false;

        // Чтобы органы внутри персонажей не становились артефактами
        if (_organs.HasComp(ent))
            return false;

        return true;
    }

    private void MakeArtifact(Entity<ItemComponent> ent)
    {
        EntityManager.AddComponents(ent, _baseParentPrototype!.Components, false);
    }

    /// <summary>
    /// Удаляет старые артефакты.
    /// Теперь только предметы
    /// </summary>
    private void DeleteOldArtifacts()
    {
        var query = AllEntityQuery<XenoArtifactComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }
    }

    #endregion

    /// <summary>
    /// Обрабатывает изменение текущих настроек сервера.
    /// Поддерживает переключение в моменте игры
    /// </summary>
    private void OnCvarChanged(bool enabled)
    {
        if (_enabled == enabled)
            return;

        if (!enabled)
        {
            var items = _helpers.GetAll<XenoArtifactRandomItemMarkerComponent>();

            foreach (var item in items)
            {
                RemComp<XenoArtifactComponent>(item);
            }
        }
        else
        {
            CreateRandomArtifacts();
        }

        _enabled = enabled;
    }
}
