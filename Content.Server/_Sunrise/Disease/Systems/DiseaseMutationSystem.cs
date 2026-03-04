// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Shared.Verbs;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared._Sunrise.TimeWindow;
using Robust.Shared.Random;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Destructible;
using Content.Shared._Sunrise.Disease.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared._Sunrise.Disease;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class DiseaseMutationSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    private ISawmill _sawmill = default!;

    /// <summary>
    ///     Зона поражения после разрушения сущности.
    /// </summary>
    private const float RangeInfectAfteDest = 10f;

    /// <summary>
    ///     Список всех body и симптомов, да, при загрузке прототипа body его тут не будет.
    /// </summary>
    private List<ProtoId<BodyPrototype>> _allBodyCache = new();
    private List<ProtoId<DiseaseSymptomPrototype>> _allSymptomsCache = new();


    /// <summary>
    ///     Сколько попыток за цикл симптомы будут мутировать.
    /// </summary>
    private const int MutateAttempts = 5;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("DiseaseMutationSystem");

        foreach (var proto in _prototype.EnumeratePrototypes<BodyPrototype>())
        {
            if (!BaseDiseaseSettings.BodyBlackList.Contains(proto.ID))
                _allBodyCache.Add(proto.ID);
        }

        foreach (var proto in _prototype.EnumeratePrototypes<DiseaseSymptomPrototype>())
            _allSymptomsCache.Add(proto.ID);

        SubscribeLocalEvent<DiseaseMutationComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DiseaseMutationComponent, GetVerbsEvent<Verb>>(DoSetVerbs);
        SubscribeLocalEvent<DiseaseMutationComponent, DestructionEventArgs>(OnDestr);
        SubscribeLocalEvent<DiseaseMutationComponent, CauseDiseaseEvent>(OnCauseDisease);
        SubscribeLocalEvent<DiseaseMutationComponent, CureDiseaseEvent>(OnCureDisease);
        SubscribeLocalEvent<DiseaseMutationComponent, ProbInfectAttemptEvent>(OnProbInfectAttempt);
    }

    private void OnInit(EntityUid uid, DiseaseMutationComponent component, ComponentInit args)
    {
        _timedWindowSystem.Reset(component.UpdateWindow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseMutationComponent, DiseaseComponent>();
        while (query.MoveNext(out var uid, out var component, out var disease))
        {
            if (!_timedWindowSystem.IsExpired(component.UpdateWindow))
                continue;

            _timedWindowSystem.Reset(component.UpdateWindow);
            ProbMutate((uid, component, disease));
        }
    }

    private void OnProbInfectAttempt(EntityUid uid, DiseaseMutationComponent component, ProbInfectAttemptEvent args)
    {
        if (HasComp<DiseaseComponent>(uid))
            args.Cancel = true;
    }

    private void OnCauseDisease(Entity<DiseaseMutationComponent> entity, ref CauseDiseaseEvent args)
    {
        UpdateAppearance(entity, entity.Comp, true);
    }

    private void OnCureDisease(Entity<DiseaseMutationComponent> entity, ref CureDiseaseEvent args)
    {
        UpdateAppearance(entity, entity.Comp, false);
    }

    private void OnDestr(Entity<DiseaseMutationComponent> entity, ref DestructionEventArgs args)
    {
        if (!TryComp<DiseaseComponent>(entity, out var disease))
            return;

        _disease.InfectAround((entity, disease), RangeInfectAfteDest);
    }

    private void DoSetVerbs(EntityUid uid, DiseaseMutationComponent component, GetVerbsEvent<Verb> args)
    {
        if (!HasComp<ComplexInteractionComponent>(args.User) || !HasComp<HandsComponent>(args.User))
            return;

        if (!TryComp<DiseaseComponent>(uid, out var disease))
            return;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("disease-mutation-verb"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () =>
            {
                _disease.ProbInfect(disease.Data, args.User);
                _disease.CureDisease(uid, disease);
            },
            Impact = LogImpact.Medium
        });

    }

    public void ProbMutate(Entity<DiseaseMutationComponent?, DiseaseComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return;

        if (!Resolve(host, ref host.Comp2, false))
            return;

        if (!CanMutate((host, host.Comp1, host.Comp2)))
            return;

        // Попытка мутации симптома
        MutateSymptom((host, host.Comp1, host.Comp2));

        // Попытка мутации расы
        MutateBody((host, host.Comp1, host.Comp2));
    }

    private void MutateSymptom(Entity<DiseaseMutationComponent?, DiseaseComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return;

        if (!Resolve(host, ref host.Comp2, false))
            return;

        // список доступных симптомов = те, которых ещё нет в вирусе
        var available = _allSymptomsCache
            .Where(protoId =>
            {
                // возвращаем те, которых ещё нет
                return !host.Comp2.Data.ActiveSymptom.Contains(protoId.Id);
            })
            .ToList();

        if (available.Count == 0)
            return;

        bool needRefresh = false;

        for (int i = 0; i < MutateAttempts; i++)
        {
            if (available.Count == 0)
                break;

            int index = _random.Next(available.Count);

            if (!_prototype.TryIndex(available[index], out var proto))
                continue;

            var price = _disease.GetSymptomPrice(host.Comp2.Data, proto);
            if (host.Comp2.Data.MutationPoints < price)
                continue;

            host.Comp2.Data.ActiveSymptom.Add(available[index]);

            host.Comp2.Data.MutationPoints -= price;

            _sawmill.Debug(
                $"Попытка мутации #{i + 1}: добавлен новый симптом '{proto.ID}' ({proto.Name}) " +
                $"ТекущиеСимптомы=[{string.Join(", ", host.Comp2.Data.ActiveSymptom)}]"
            );

            available.RemoveAt(index); // удаляем выбранный симптом
            needRefresh = true;
        }

        if (needRefresh)
            _disease.RefreshSymptoms((host, host.Comp2));

    }

    private void MutateBody(Entity<DiseaseMutationComponent?, DiseaseComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return;

        if (!Resolve(host, ref host.Comp2, false))
            return;

        var available = _allBodyCache
            .Where(s => !host.Comp2.Data.BodyWhitelist.Contains(s))
            .ToList();

        if (available.Count == 0)
            return;

        var price = _disease.GetBodyPrice(host.Comp2.Data);
        if (host.Comp2.Data.MutationPoints < price)
            return;

        var pick = _random.Pick(available);
        host.Comp2.Data.BodyWhitelist.Add(pick);

        host.Comp2.Data.MutationPoints -= price;

        _sawmill.Debug(
            $"Добавлена новая раса: '{pick}'. " +
            $"Штамм='{host.Comp2.Data.StrainId}'. " +
            $"ТекущийWhitelist=[{string.Join(", ", host.Comp2.Data.BodyWhitelist)}]"
        );
    }

    public bool CanMutate(Entity<DiseaseMutationComponent?, DiseaseComponent?> host)
    {
        if (!Resolve(host, ref host.Comp1, false))
            return false;

        if (!Resolve(host, ref host.Comp2, false))
            return false;

        if (!HasComp<DiseaseComponent>(host))
            return false;

        // Если есть состояния, значит мутирует только живой
        if (TryComp<MobStateComponent>(host, out var mobState))
            return !_mobState.IsDead(host, mobState);

        return true;
    }

    private void UpdateAppearance(EntityUid uid, DiseaseMutationComponent component, bool isInfected)
    {
        if (!component.ChangeApperance)
            return;

        if (isInfected)
        {
            _appearance.SetData(uid, DiseaseMutationVisuals.state, false);
            _appearance.SetData(uid, DiseaseMutationVisuals.infected, true);
        }
        else
        {
            _appearance.SetData(uid, DiseaseMutationVisuals.state, true);
            _appearance.SetData(uid, DiseaseMutationVisuals.infected, false);
        }
    }
}
