// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Random;
using Content.Shared._Sunrise.Disease;
using Content.Server.Store.Systems;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Robust.Shared.Timing;
using Content.Shared.Zombies;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server._Sunrise.Disease;

public sealed class DiseaseRoleSystem : SharedDiseaseRoleSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;


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
        
        // Subscribe to death events to reward disease points
        SubscribeLocalEvent<DiseaseRoleComponent, EntityTerminatingEvent>(OnDiseaseDeath);
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
        if (component.CoughSneezeInfectChance < 0.85f)
            component.CoughSneezeInfectChance += 0.05f;
        else
        {
            component.CoughSneezeInfectChance = 1;
            _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
        }
    }


    private void OnInfects(InfectEvent args)
    {
        if (TryComp<DiseaseRoleComponent>(args.Performer, out var component))
        {
            // The action system automatically consumes charges, so we don't need to do it manually
            // Just play the audio and perform the infection

            // Play Initial Infected antag audio (only for the disease player)
            _audio.PlayGlobal("/Audio/Ambience/Antag/zombie_start.ogg", args.Performer);

            OnInfect(args, 1);
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
        component.Symptoms.Add("Headache", new SymptomData(1, 4));
    }

    private void OnShop(EntityUid uid, DiseaseRoleComponent component, DiseaseShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

        private void OnDiseaseInfo(EntityUid uid, DiseaseRoleComponent component, DiseaseInfoEvent args)
    {
        // Create a simple formatted display
        var infoText = Loc.GetString("disease-info-header") + "\n\n";

        // Core Statistics Section
        infoText += Loc.GetString("disease-info-core-statistics") + ":\n";
        infoText += "├─ " + Loc.GetString("disease-info-base-chance") + ": " + (component.BaseInfectChance * 100).ToString("F0") + "%\n";
        infoText += "├─ " + Loc.GetString("disease-info-cough-sneeze-chance") + ": " + (component.CoughSneezeInfectChance * 100).ToString("F0") + "%\n";
        infoText += "├─ " + Loc.GetString("disease-info-lethal") + ": " + component.Lethal + "\n";
        infoText += "└─ " + Loc.GetString("disease-info-shield") + ": " + component.Shield + "\n\n";

        // Infection Statistics Section
        infoText += Loc.GetString("disease-info-infection-statistics") + ":\n";
        infoText += "├─ " + Loc.GetString("disease-info-infected-count") + ": " + component.Infected.Count + "\n";
        infoText += "└─ " + Loc.GetString("disease-info-total-infected") + ": " + component.SickOfAllTime;

        _popup.PopupEntity(infoText, uid, PopupType.Large);
    }



        private void OnStorePurchase(ref StoreBuyFinishedEvent args)
    {
        // Check if this is the InfectCharge purchase
        if (args.PurchasedItem.ID == "InfectCharge")
        {
            // The store owner (disease antagonist) is the one who gets the charges
            var storeOwner = args.StoreUid;

            if (!EntityManager.TryGetComponent<DiseaseRoleComponent>(storeOwner, out var diseaseComp))
                return;

            // Find the Infect action and check current charges
            if (EntityManager.TryGetComponent<ActionsComponent>(storeOwner, out var actionsComp))
            {
                foreach (var actionUid in actionsComp.Actions)
                {
                    // Check if this action is the Infect action by looking for EntityTargetActionComponent
                    if (HasComp<EntityTargetActionComponent>(actionUid) &&
                        HasComp<LimitedChargesComponent>(actionUid))
                    {
                        var chargesComp = Comp<LimitedChargesComponent>(actionUid);
                        var currentCharges = _sharedCharges.GetCurrentCharges((actionUid, chargesComp));

                        // Check if already at max charges (3)
                        if (currentCharges >= 3)
                        {
                            // Refund the purchase since they can't use more charges
                            AddMoney(storeOwner, 10);
                            _popup.PopupEntity(Loc.GetString("disease-infect-charge-max-reached"), storeOwner, PopupType.Medium);
                        }
                        else
                        {
                            // Add 1 charge
                            _sharedCharges.AddCharges((actionUid, chargesComp), 1);
                            // Show success message
                            _popup.PopupEntity(Loc.GetString("disease-infect-charge-purchased"), storeOwner, PopupType.Medium);
                        }
                        break;
                    }
                }
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
            component.Symptoms.Add(args.Symptom, new SymptomData(args.MinLevel, args.MaxLevel));
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

    private void OnDiseaseDeath(EntityUid uid, DiseaseRoleComponent component, EntityTerminatingEvent args)
    {
        // When a disease antagonist dies, reward all disease antagonists with 10 points
        var diseaseQuery = EntityQueryEnumerator<DiseaseRoleComponent>();
        while (diseaseQuery.MoveNext(out var diseaseUid, out var diseaseComp))
        {
            if (diseaseUid != uid) // Don't reward the dying one
            {
                AddMoney(diseaseUid, 10);
                _popup.PopupEntity(Loc.GetString("disease-death-reward"), diseaseUid, PopupType.Medium);
            }
        }
    }
}
