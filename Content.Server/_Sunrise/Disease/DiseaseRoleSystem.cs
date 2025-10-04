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
using System.Text;
using Content.Shared.Mobs;

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

    private static readonly string[] _bloodReagents = { "DiseaseBloodFirst", "DiseaseBloodSecond", "DiseaseBloodThird" };

    [ValidatePrototypeId<EntityPrototype>] private const string DiseaseShopId = "ActionDiseaseShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseShopActionEvent>(OnShop);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddSymptomEvent>(OnAddSymptom);
        SubscribeLocalEvent<InfectEvent>(OnInfects);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseInfoEvent>(OnDiseaseInfo);


        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseZombieEvent>(OnZombie);

        // Subscribe to store purchase events
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStorePurchase);

        // Subscribe to death events to reward disease points
        SubscribeLocalEvent<MobStateChangedEvent>(OnInfectedDeath);
    }




    private void OnInfects(InfectEvent args)
    {
        if (TryComp<DiseaseRoleComponent>(args.Performer, out var component))
        {
            if (TryRemoveMoney(args.Performer, component.InfectCost))
                OnInfect(args, 1.0f);
            else
            {
                _popup.PopupEntity(Loc.GetString("disease-not-enough-evolution-points"), args.Performer, PopupType.Medium);
                return;
            }

            // Play Initial Infected antag audio (only for the disease player)
            _audio.PlayGlobal("/Audio/Ambience/Antag/zombie_start.ogg", args.Performer);
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
        component.NewBloodReagent = _random.Pick(_bloodReagents);
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
        // Create a simple formatted display using StringBuilder for better performance
        var infoText = new StringBuilder();
        infoText.AppendLine(Loc.GetString("disease-info-header"));
        infoText.AppendLine();

        // Core Statistics Section
        infoText.AppendLine(Loc.GetString("disease-info-core-statistics") + ":");
        infoText.AppendLine("├─ " + Loc.GetString("disease-info-base-chance") + ": " + (component.BaseInfectChance * 100).ToString("F0") + "%");
        infoText.AppendLine("├─ " + Loc.GetString("disease-info-cough-sneeze-chance") + ": " + (component.CoughSneezeInfectChance * 100).ToString("F0") + "%");
        infoText.AppendLine("├─ " + Loc.GetString("disease-info-lethal") + ": " + component.Lethal);
        infoText.AppendLine("└─ " + Loc.GetString("disease-info-shield") + ": " + component.Shield);
        infoText.AppendLine();

        // Infection Statistics Section
        infoText.AppendLine(Loc.GetString("disease-info-infection-statistics") + ":");
        infoText.AppendLine("├─ " + Loc.GetString("disease-info-infected-count") + ": " + component.Infected.Count);
        infoText.Append("└─ " + Loc.GetString("disease-info-total-infected") + ": " + component.SickOfAllTime);

        _popup.PopupEntity(infoText.ToString(), uid, PopupType.Large);
    }



    private void OnStorePurchase(ref StoreBuyFinishedEvent args)
    {
        // The store owner (disease antagonist) is the one who gets the upgrades
        var storeOwner = args.StoreUid;

        if (!TryComp<DiseaseRoleComponent>(storeOwner, out var diseaseComp))
            return;

        // Handle different purchase types
        switch (args.PurchasedItem.ID)
        {
            case "InfectCharge":
                // Find the Infect action and check current charges
                if (TryComp<ActionsComponent>(storeOwner, out var actionsComp))
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
                                _popup.PopupEntity(Loc.GetString("disease-infect-charge-max-reached", ("maxCharges", 3)), storeOwner, PopupType.Medium);
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
                break;

            case "BaseChance":
                if (diseaseComp.BaseInfectChance < 0.9f)
                {
                    diseaseComp.BaseInfectChance += 0.1f;
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-purchased"), storeOwner, PopupType.Medium);
                }
                else
                {
                    diseaseComp.BaseInfectChance = 1;
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-max-reached"), storeOwner, PopupType.Medium);
                }
                break;

            case "InfectChance":
                if (diseaseComp.CoughSneezeInfectChance < 0.85f)
                {
                    diseaseComp.CoughSneezeInfectChance += 0.05f;
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-purchased"), storeOwner, PopupType.Medium);
                }
                else
                {
                    diseaseComp.CoughSneezeInfectChance = 1;
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-max-reached"), storeOwner, PopupType.Medium);
                }
                break;

            case "Shield":
                if (diseaseComp.Shield < 6)
                {
                    diseaseComp.Shield += 1;
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-purchased"), storeOwner, PopupType.Medium);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-max-reached"), storeOwner, PopupType.Medium);
                }
                break;

            case "Lethal":
                if (diseaseComp.Lethal < 5)
                {
                    diseaseComp.Lethal += 1;
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-purchased"), storeOwner, PopupType.Medium);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("disease-upgrade-max-reached"), storeOwner, PopupType.Medium);
                }
                break;
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
        var convertedCount = 0;

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
                convertedCount++;
            }
        }

        // Show success message with count
        if (convertedCount > 0)
        {
            _popup.PopupEntity(Loc.GetString("disease-zombie-success", ("count", convertedCount)), uid, PopupType.Medium);
        }

        // Remove the zombie action after use
        _actionsSystem.RemoveAction((uid, null), args.Action.Owner);
    }

    private void OnInfectedDeath(MobStateChangedEvent args)
    {
        // Only trigger when the entity actually dies (not when they're revived)
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead)
            return;

        // Safety check - ensure the entity is still valid
        if (Deleted(args.Target))
            return;

        // Reward all other disease antagonists when any infected dies
        var diseaseQuery = EntityQueryEnumerator<DiseaseRoleComponent>();
        while (diseaseQuery.MoveNext(out var diseaseUid, out var diseaseComp))
        {
            if (diseaseUid == args.Target) // Don't reward the dying one
                continue;

            // Safety check - ensure the target entity is still valid
            if (Deleted(diseaseUid))
                continue;

            // Award reward
            AddMoney(diseaseUid, 10);
            _popup.PopupEntity(Loc.GetString("disease-death-reward", ("points", 10)), diseaseUid, PopupType.Medium);
        }
    }
}
