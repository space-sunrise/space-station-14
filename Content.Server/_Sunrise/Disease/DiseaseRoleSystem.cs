// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Random;
using Content.Shared._Sunrise.Disease;
using Content.Server.Store.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Store.Components;

namespace Content.Server._Sunrise.Disease;

public sealed class DiseaseRoleSystem : SharedDiseaseRoleSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;


    [ValidatePrototypeId<EntityPrototype>] private const string DiseaseShopId = "ActionDiseaseShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseShopActionEvent>(OnShop);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddSymptomEvent>(OnAddSymptom);
        SubscribeLocalEvent<InfectEvent>(OnInfects);

        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddBaseChanceEvent>(OnBaseChance);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddCoughChanceEvent>(OnCoughChance);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddLethalEvent>(OnLethal);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddShieldEvent>(OnShield);
    }

    private void OnLethal(EntityUid uid, DiseaseRoleComponent component, DiseaseAddLethalEvent args)
    {
        if (!TryRemoveMoney(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, uid);
            return;
        }
        component.Lethal += 1;
        if (component.Lethal >= 5)
        {
            _actionsSystem.RemoveAction(uid, args.Action);
        }
    }

    private void OnShield(EntityUid uid, DiseaseRoleComponent component, DiseaseAddShieldEvent args)
    {
        if (!TryRemoveMoney(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, uid);
            return;
        }
        component.Shield += 1;
        if (component.Shield >= 6)
        {
            _actionsSystem.RemoveAction(uid, args.Action);
        }
    }

    private void OnBaseChance(EntityUid uid, DiseaseRoleComponent component, DiseaseAddBaseChanceEvent args)
    {
        if (!TryRemoveMoney(uid, 20))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, uid);
            return;
        }
        if (component.BaseInfectChance < 0.9f)
            component.BaseInfectChance += 0.1f;
        else
        {
            component.BaseInfectChance = 1;
            _actionsSystem.RemoveAction(uid, args.Action);
        }
    }

    private void OnCoughChance(EntityUid uid, DiseaseRoleComponent component, DiseaseAddCoughChanceEvent args)
    {
        if (!TryRemoveMoney(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, uid);
            return;
        }
        if (component.CoughInfectChance < 0.85f)
            component.CoughInfectChance += 0.05f;
        else
        {
            component.CoughInfectChance = 1;
            _actionsSystem.RemoveAction(uid, args.Action);
        }
    }


    private void OnInfects(InfectEvent args)
    {
        if (TryComp<DiseaseRoleComponent>(args.Performer, out var component))
        {
            if (component.FreeInfects > 0)
            {
                component.FreeInfects--;
                OnInfect(args, 1);
            }
            else if (TryRemoveMoney(args.Performer, component.InfectCost))
            {
                OnInfect(args);
            }
        }
    }

    private void OnMapInit(EntityUid uid, DiseaseRoleComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, DiseaseShopId, uid);
        foreach (var (id, charges) in component.Actions)
        {
            EntityUid? actionId = null;
            if (_actionsSystem.AddAction(uid, ref actionId, id))
                _actionsSystem.SetCharges(actionId, charges < 0 ? null : charges);
        }
        component.NewBloodReagent = _random.Pick(new List<string>() { "DiseaseBloodFirst", "DiseaseBloodSecond", "DiseaseBloodThird" });
        component.Symptoms.Add("Headache", (1, 4));
    }

    private void OnShop(EntityUid uid, DiseaseRoleComponent component, DiseaseShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    void AddMoney(EntityUid uid, FixedPoint2 value)
    {
        if (TryComp<DiseaseRoleComponent>(uid, out var diseaseComp))
        {
            if (TryComp<StoreComponent>(uid, out var store))
            {
                bool f = _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                    {
                        {diseaseComp.CurrencyPrototype, value}
                    }, uid);
                _store.UpdateUserInterface(uid, uid, store);
            }
        }
    }

    bool TryRemoveMoney(EntityUid uid, FixedPoint2 value)
    {
        if (TryComp<DiseaseRoleComponent>(uid, out var diseaseComp))
        {
            if (TryComp<StoreComponent>(uid, out var store))
            {
                if (store.Balance[diseaseComp.CurrencyPrototype] >= value)
                {
                    _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                    {
                        {diseaseComp.CurrencyPrototype, -value}
                    }, uid);
                    _store.UpdateUserInterface(uid, uid, store);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        return false;
    }

    private void OnAddSymptom(EntityUid uid, DiseaseRoleComponent component, DiseaseAddSymptomEvent args)
    {
        if (!component.Symptoms.ContainsKey(args.Symptom))
        {
            component.Symptoms.Add(args.Symptom, (args.MinLevel, args.MaxLevel));
        }
        _actionsSystem.RemoveAction(uid, args.Action);
    }

    //private void OnZombie(EntityUid uid, DiseaseRoleComponent component, DiseaseZombieEvent args)
    //{
    //    var inf = component.Infected.ToArray();
    //    foreach(EntityUid infected in inf)
    //    {
    //        if (_random.Prob(0.8f)) {
    //            RemComp<SickComponent>(infected);
    //            component.Infected.Remove(infected);
    //            EnsureComp<ZombifyOnDeathComponent>(infected);
    //            EnsureComp<PendingZombieComponent>(infected);
    //        }
    //    }
    //}

}
