// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Random;
using Content.Shared._Sunrise.Disease;
using Content.Server.Store.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Robust.Shared.Timing;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Content.Shared.Zombies;
using Content.Shared.Chemistry.Components;

namespace Content.Server._Sunrise.Disease;

public sealed class DiseaseRoleSystem : SharedDiseaseRoleSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;


    [ValidatePrototypeId<EntityPrototype>] private const string DiseaseShopId = "ActionDiseaseShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseShopActionEvent>(OnShop);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddSymptomEvent>(OnAddSymptom);
        SubscribeLocalEvent<InfectEvent>(OnInfects);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseInfoEvent>(OnDiseaseInfo);

        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddBaseChanceEvent>(OnBaseChance);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddCoughChanceEvent>(OnCoughChance);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddLethalEvent>(OnLethal);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddShieldEvent>(OnShield);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseZombieEvent>(OnZombie);

                // Subscribe to store purchase events
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStorePurchase);
    }

    private void OnLethal(EntityUid uid, DiseaseRoleComponent component, DiseaseAddLethalEvent args)
    {
        if (!TryRemoveMoney(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, PopupType.Medium);
            return;
        }
        component.Lethal += 1;
        if (component.Lethal >= 5)
        {
            _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
        }
    }

    private void OnShield(EntityUid uid, DiseaseRoleComponent component, DiseaseAddShieldEvent args)
    {
        if (!TryRemoveMoney(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, PopupType.Medium);
            return;
        }
        component.Shield += 1;
        if (component.Shield >= 6)
        {
            _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
        }
    }

    private void OnBaseChance(EntityUid uid, DiseaseRoleComponent component, DiseaseAddBaseChanceEvent args)
    {
        if (!TryRemoveMoney(uid, 20))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, PopupType.Medium);
            return;
        }
        if (component.BaseInfectChance < 0.9f)
            component.BaseInfectChance += 0.1f;
        else
        {
            component.BaseInfectChance = 1;
            _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
        }
    }

    private void OnCoughChance(EntityUid uid, DiseaseRoleComponent component, DiseaseAddCoughChanceEvent args)
    {
        if (!TryRemoveMoney(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), uid, PopupType.Medium);
            return;
        }
        if (component.CoughInfectChance < 0.85f)
            component.CoughInfectChance += 0.05f;
        else
        {
            component.CoughInfectChance = 1;
            _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
        }
    }


    private void OnInfects(InfectEvent args)
    {
        if (TryComp<DiseaseRoleComponent>(args.Performer, out var component))
        {
            // Check if the Infect action has charges
            if (EntityManager.TryGetComponent<ActionsComponent>(args.Performer, out var actionsComp))
            {
                foreach (var actionUid in actionsComp.Actions)
                {
                    if (HasComp<EntityTargetActionComponent>(actionUid) &&
                        HasComp<LimitedChargesComponent>(actionUid))
                    {
                        var chargesComp = Comp<LimitedChargesComponent>(actionUid);
                        var currentCharges = _sharedCharges.GetCurrentCharges((actionUid, chargesComp));

                        if (currentCharges > 0)
                        {
                            // Use a charge
                            _sharedCharges.AddCharges((actionUid, chargesComp), -1);
                            OnInfect(args, 1);
                            return;
                        }
                        else
                        {
                            // No charges left, try to use money
                            if (TryRemoveMoney(args.Performer, component.InfectCost))
                            {
                                OnInfect(args);
                                return;
                            }
                            else
                            {
                                // No charges and no money
                                return;
                            }
                        }
                    }
                }
            }

            // Fallback: try to use money if no charges found
            if (TryRemoveMoney(args.Performer, component.InfectCost))
            {
                OnInfect(args);
            }
        }
    }

    private void OnMapInit(EntityUid uid, DiseaseRoleComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, DiseaseShopId, uid);
        // Add starting actions with charges
        foreach (var (id, charges) in component.Actions)
        {
            EntityUid? actionId = null;
            if (_actionsSystem.AddAction(uid, ref actionId, id))
            {
                var limitCharges = EnsureComp<LimitedChargesComponent>(actionId.Value);
                _sharedCharges.SetCharges((actionId.Value, limitCharges), charges);
            }
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

    private void OnDiseaseInfo(EntityUid uid, DiseaseRoleComponent component, DiseaseInfoEvent args)
    {
        // Create a medical scanner-style formatted display
        var infoText = "[color=#00ff00][bold]" + Loc.GetString("disease-info-header") + "[/bold][/color]\n\n";

        // Core Statistics Section
        infoText += "[color=#ffff00][bold]Core Statistics:[/bold][/color]\n";
        infoText += "├─ " + Loc.GetString("disease-info-base-chance", ("value", (component.BaseInfectChance * 100).ToString("F0"))) + "\n";
        infoText += "├─ " + Loc.GetString("disease-info-cough-chance", ("value", (component.CoughInfectChance * 100).ToString("F0"))) + "\n";
        infoText += "├─ " + Loc.GetString("disease-info-lethal", ("value", component.Lethal)) + "\n";
        infoText += "└─ " + Loc.GetString("disease-info-shield", ("value", component.Shield)) + "\n\n";

        // Infection Statistics Section
        infoText += "[color=#00ffff][bold]Infection Statistics:[/bold][/color]\n";
        infoText += "├─ " + Loc.GetString("disease-info-infected-count", ("value", component.Infected.Count)) + "\n";
        infoText += "└─ " + Loc.GetString("disease-info-total-infected", ("value", component.SickOfAllTime)) + "\n\n";

        // Currency Section
        if (TryComp<StoreComponent>(uid, out var store))
        {
            infoText += "[color=#ff00ff][bold]Resources:[/bold][/color]\n";
            infoText += "└─ " + Loc.GetString("disease-info-disease-points", ("value", store.Balance[component.CurrencyPrototype]));
        }

        _popup.PopupEntity(infoText, uid, uid, PopupType.Large);
    }



        private void OnStorePurchase(ref StoreBuyFinishedEvent args)
    {
        // Check if this is the InfectCharge purchase
        if (args.PurchasedItem.ID == "InfectCharge")
        {
            // The store owner (disease antagonist) is the one who gets the charges
            var storeOwner = args.StoreUid;

            // Debug logging
            Log.Debug($"InfectCharge purchase completed for store {ToPrettyString(storeOwner)}");

                        if (!EntityManager.TryGetComponent<DiseaseRoleComponent>(storeOwner, out var diseaseComp))
                return;

            // Find the Infect action and check current charges
            if (EntityManager.TryGetComponent<ActionsComponent>(storeOwner, out var actionsComp))
            {
                Log.Debug($"Found ActionsComponent with {actionsComp.Actions.Count} actions");

                foreach (var actionUid in actionsComp.Actions)
                {
                    Log.Debug($"Checking action {ToPrettyString(actionUid)}");

                    // Check if this action is the Infect action by looking for EntityTargetActionComponent
                    if (HasComp<EntityTargetActionComponent>(actionUid) &&
                        HasComp<LimitedChargesComponent>(actionUid))
                    {
                        Log.Debug($"Found Infect action with charges: {ToPrettyString(actionUid)}");

                        var chargesComp = Comp<LimitedChargesComponent>(actionUid);
                        var currentCharges = _sharedCharges.GetCurrentCharges((actionUid, chargesComp));

                        Log.Debug($"Current charges: {currentCharges}");

                        // Add 1 charge (no limit)
                        _sharedCharges.AddCharges((actionUid, chargesComp), 1);
                        Log.Debug($"Added 1 charge, new total: {_sharedCharges.GetCurrentCharges((actionUid, chargesComp))}");

                        // Show success message
                        _popup.PopupEntity(Loc.GetString("disease-infect-charge-purchased"), storeOwner, PopupType.Medium);
                        break;
                    }
                }
            }
            else
            {
                Log.Debug("No ActionsComponent found on buyer");
            }
        }
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
        _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
    }

    private void OnZombie(EntityUid uid, DiseaseRoleComponent component, DiseaseZombieEvent args)
    {
        var infected = component.Infected.ToArray();

        for (int i = 0; i < infected.Length; i++)
        {
            var target = infected[i];
            if (target.IsValid() && !Deleted(target))
            {
                // Remove sick component and add zombie components
                RemComp<SickComponent>(target);
                component.Infected.Remove(target);

                // Add zombie components
                EnsureComp<ZombifyOnDeathComponent>(target);
                EnsureComp<PendingZombieComponent>(target);
            }
        }

        // Remove the zombie action after use
        _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
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
