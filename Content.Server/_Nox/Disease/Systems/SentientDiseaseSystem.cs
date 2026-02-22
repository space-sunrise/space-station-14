// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease;
using Robust.Server.GameObjects;
using Content.Shared._Nox.Disease.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Nox.Disease.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared.Actions;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared._Nox.TimeWindow;

namespace Content.Server._Nox.Disease.Systems;

public sealed class SentientDiseaseSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    private const int PrimaryPacientPrice = 1000;
    private const int ModifyPointsRegenPerInfected = 2;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SentientDiseaseComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SentientDiseaseComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SentientDiseaseComponent, ShopMutationActionEvent>(OnShopMutation);
        SubscribeLocalEvent<SentientDiseaseComponent, SelectPrimaryPatientEvent>(OnSelectPrimaryPatient);
        SubscribeLocalEvent<SentientDiseaseComponent, TeleportToPrimaryPatientEvent>(OnTeleportToPrimaryPatient);
        SubscribeLocalEvent<SentientDiseaseComponent, EvolutionConsoleUiButtonPressedMessage>(OnButtonPressed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SentientDiseaseComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timedWindowSystem.IsExpired(component.UpdateWindow))
            {
                _timedWindowSystem.Reset(component.UpdateWindow);
                UpdateSentientDisease(uid, component);
            }
        }
    }

    private void OnTeleportToPrimaryPatient(EntityUid uid, SentientDiseaseComponent component, TeleportToPrimaryPatientEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var list = component.CurrentPrimaryInfected;

        if (list.Count <= 0)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("sentient-disease-teleport-no-primary-infected"),
                uid,
                uid,
                PopupType.Medium
            );
            return;
        }

        if (component.SelectedPrimaryInfected >= list.Count)
            component.SelectedPrimaryInfected = 0;

        var target = list[component.SelectedPrimaryInfected];
        var entityCoords = Transform(target).Coordinates;

        _transform.SetCoordinates(uid, entityCoords);
        _transform.AttachToGridOrMap(uid);

        component.SelectedPrimaryInfected++;
    }

    private void OnSelectPrimaryPatient(EntityUid uid, SentientDiseaseComponent component, SelectPrimaryPatientEvent args)
    {
        if (args.Target == uid)
            return;

        if (component.Data == null)
            return;

        args.Handled = true;

        if (TryComp<DiseaseComponent>(args.Target, out var disease)
            && disease.Data.StrainId == component.Data.StrainId)
        {
            AddPrimaryPatient(uid, args.Target, component);
        }
        else if (!_diseaseSystem.CanInfect(args.Target, component.Data))
        {
            _popupSystem.PopupEntity(
                    Loc.GetString("sentient-disease-infect-impossible-target"),
                    uid,
                    uid,
                    PopupType.Medium);

            return;
        }

        AddPrimaryPatient(uid, args.Target, component);
    }

    private void AddPrimaryPatient(EntityUid uid, EntityUid target, SentientDiseaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Data == null)
            return;

        var totalPrice = PrimaryPacientPrice * component.FactPrimaryInfected;
        var missingPoints2 = totalPrice - component.Data.MutationPoints;

        if (component.Data.MutationPoints < totalPrice)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("sentient-disease-infect-no-points", ("price", missingPoints2)),
                uid,
                uid,
                PopupType.Medium
            );
            return;
        }

        if (TryAddPrimaryInfected(uid, target, component))
            component.Data.MutationPoints -= totalPrice;
        else
            _popupSystem.PopupEntity(Loc.GetString("sentient-disease-infect-failed-source"), uid, uid, PopupType.Medium);
    }

    private void UpdateSentientDisease(EntityUid uid, SentientDiseaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Data == null)
            return;

        component.Data.MutationPoints += component.Data.RegenMutationPoints + _diseaseSystem.GetQuantityInfected(component.Data.StrainId) * ModifyPointsRegenPerInfected;
    }

    private void OnButtonPressed(EntityUid uid, SentientDiseaseComponent component, EvolutionConsoleUiButtonPressedMessage args)
    {
        switch (args.Button)
        {
            case EvolutionConsoleUiButton.EvolutionSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<DiseaseSymptomPrototype>(args.Symptom, out var proto)
                        || component.Data == null)
                        return;

                    var price = _diseaseSystem.GetSymptomPrice(component.Data, proto);
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.ActiveSymptom.Add(args.Symptom);

                    var symptomInstance = _diseaseSystem.CreateSymptomInstance(proto);
                    symptomInstance.ApplyDataEffect(component.Data, add: true);

                    UpdateDiseaseDataForStrain(uid, component);

                    break;
                }
            case EvolutionConsoleUiButton.EvolutionBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || component.Data == null)
                        return;

                    var price = _diseaseSystem.GetBodyPrice(component.Data);
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.BodyWhitelist.Add(args.Body);
                    UpdateDiseaseDataForStrain(uid, component);
                    break;
                }
            case EvolutionConsoleUiButton.DeleteSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<DiseaseSymptomPrototype>(args.Symptom, out var proto)
                        || component.Data == null)
                        return;

                    var price = _diseaseSystem.GetSymptomDeletePrice(component.Data.MultiPriceDeleteSymptom);
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.ActiveSymptom.Remove(args.Symptom);

                    var symptomInstance = _diseaseSystem.CreateSymptomInstance(proto);
                    symptomInstance.ApplyDataEffect(component.Data, add: false);

                    UpdateDiseaseDataForStrain(uid, component);

                    break;
                }
            case EvolutionConsoleUiButton.DeleteBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || component.Data == null)
                        return;

                    var price = _diseaseSystem.GetBodyDeletePrice();
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.BodyWhitelist.Remove(args.Body);
                    UpdateDiseaseDataForStrain(uid, component);
                    break;
                }
            default:
                break;
        }

        UpdateUserInterface((uid, component));
    }

    /// <summary>
    ///     Обновляет данные всех DiseaseComponent с данным StrainId.
    /// </summary>
    public void UpdateDiseaseDataForStrain(EntityUid uid, SentientDiseaseComponent? source = null)
    {
        if (!Resolve(uid, ref source))
            return;

        if (source.Data == null)
            return;

        if (string.IsNullOrEmpty(source.Data.StrainId) || source.Data == null)
            return;

        var query = EntityQueryEnumerator<DiseaseComponent>();
        while (query.MoveNext(out var diseaseUid, out var diseaseComponent))
        {
            if (diseaseComponent.Data != null && diseaseComponent.Data.StrainId == source.Data.StrainId)
            {
                diseaseComponent.Data.ApplyInfectionData(source.Data);
                _diseaseSystem.RefreshSymptoms((diseaseUid, diseaseComponent));
            }
        }
    }

    public bool TryAddPrimaryInfected(EntityUid uid, EntityUid target, SentientDiseaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.FactPrimaryInfected >= component.MaxPrimaryInfected)
            return false;

        if (HasComp<PrimaryPacientComponent>(target))
            return false;

        if (HasComp<DiseaseComponent>(target))
            return false;

        if (component.Data == null)
            return false;

        _diseaseSystem.InfectEntity(component.Data, target);

        component.CurrentPrimaryInfected.Add(target);
        component.FactPrimaryInfected++;

        var primary = new PrimaryPacientComponent(uid, component.Data.StrainId);

        AddComp(target, primary);

        return true;
    }

    public void RemovePrimaryInfected(EntityUid uid, EntityUid target, SentientDiseaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CurrentPrimaryInfected.Remove(target);

        if (component.CurrentPrimaryInfected.Count <= 0)
            QueueDel(uid);
    }

    private void OnShopMutation(Entity<SentientDiseaseComponent> entity, ref ShopMutationActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<UserInterfaceComponent>(entity, out var userInterface))
            return;

        UpdateUserInterface((entity, entity.Comp));
        _uiSystem.OpenUi((entity, userInterface), DiseaseEvolutionConsoleUiKey.Key, entity);
    }

    private void OnInit(Entity<SentientDiseaseComponent> entity, ref ComponentInit args)
    {
        var strain = DiseaseData.GenerateStrainId();

        if (entity.Comp.Data == null)
            entity.Comp.Data = new DiseaseData(strain);
        else
            entity.Comp.Data.StrainId = strain;

        _timedWindowSystem.Reset(entity.Comp.UpdateWindow);

        _actionsSystem.AddAction(entity, ref entity.Comp.ShopMutationActionEntity, entity.Comp.ShopMutationAbility, entity);
        _actionsSystem.AddAction(entity, ref entity.Comp.SelectPrimaryPatientActionEntity, entity.Comp.SelectPrimaryPatientAbility, entity);
        _actionsSystem.AddAction(entity, ref entity.Comp.TeleportToPrimaryPatientActionEntity, entity.Comp.TeleportToPrimaryPatientAbility, entity);
    }

    private void OnShutdown(Entity<SentientDiseaseComponent> entity, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(entity.Owner, entity.Comp.ShopMutationActionEntity);
        _actionsSystem.RemoveAction(entity.Owner, entity.Comp.SelectPrimaryPatientActionEntity);
        _actionsSystem.RemoveAction(entity.Owner, entity.Comp.TeleportToPrimaryPatientActionEntity);
    }

    public void UpdateUserInterface(Entity<SentientDiseaseComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryComp<UserInterfaceComponent>(entity, out var userInterface))
            return;

        if (!_uiSystem.HasUi(entity, DiseaseEvolutionConsoleUiKey.Key, userInterface))
            return;

        var newState = GetUserInterfaceState((entity, entity.Comp));
        _uiSystem.SetUiState((entity, userInterface), DiseaseEvolutionConsoleUiKey.Key, newState);
    }

    private DiseaseEvolutionConsoleBoundUserInterfaceState GetUserInterfaceState(Entity<SentientDiseaseComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return default!;

        var data = console.Comp.Data;
        var infectivity = 0f;
        var infectedCount = data != null ? _diseaseSystem.GetQuantityInfected(data.StrainId) : 0;
        var pointsPerSecond = data != null ? data.RegenMutationPoints + infectedCount * ModifyPointsRegenPerInfected : 0;

        if (data != null)
        {
            foreach (var sympId in data.ActiveSymptom)
            {
                if (_prototypeManager.TryIndex(sympId, out var prototype))
                    infectivity += prototype.AddInfectivity;
            }
        }

        if (data != null)
        {
            foreach (var sympId in data.ActiveSymptom)
            {
                if (_prototypeManager.TryIndex(sympId, out var prototype))
                    infectivity += prototype.AddInfectivity;
            }
        }

        return new DiseaseEvolutionConsoleBoundUserInterfaceState(
            data?.MutationPoints ?? 0,
            data?.MultiPriceDeleteSymptom ?? 0,
            true,
            true,
            true,
            true,
            data != null,
            data?.ActiveSymptom,
            data?.BodyWhitelist,
            data?.MaxThreshold ?? 100f,
            infectivity,
            infectedCount,
            pointsPerSecond,
            isSentientDisease: true
        );
    }
}
