// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Ligyb;
using Content.Server.Actions;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Zombies;
using Content.Shared.FixedPoint;
using Content.Shared.Zombies;
namespace Content.Server.Ligyb;

public sealed class DiseaseRoleSystem : SharedDiseaseRoleSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string DiseaseShopId = "ActionDiseaseShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DiseaseRoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseShopActionEvent>(OnShop);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseStartCoughEvent>(OnCough);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseStartSneezeEvent>(OnSneeze);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseStartVomitEvent>(OnVomit);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseStartCryingEvent>(OnCrying);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseZombieEvent>(OnZombie);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseNarcolepsyEvent>(OnNarcolepsy);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseMutedEvent>(OnMuted);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseSlownessEvent>(OnSlowness);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseBleedEvent>(OnBleed);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseBlindnessEvent>(OnBlindness);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseInsultEvent>(OnInsult);
        SubscribeLocalEvent<InfectEvent>(OnInfects);

    }


    private void OnInfects(InfectEvent args)
    {
        if (TryComp<DiseaseRoleComponent>(args.Performer, out var component))
        {
            if(component.FreeInfects > 0)
            {
                component.FreeInfects--;
                OnInfect(args);
                return;
            }
            if (TryRemoveMoney(args.Performer, component.InfectCost))
            {
                OnInfect(args);
            }
        }
    }

    private void OnInit(EntityUid uid, DiseaseRoleComponent component, ComponentInit args)
    {

        foreach (var (id, charges) in component.Actions)
        {
            EntityUid? actionId = null;
            if (_actionsSystem.AddAction(uid, ref actionId, id))
                _actionsSystem.SetCharges(actionId, charges < 0 ? null : charges);
        }
        component.NewBloodReagent = _random.Pick(new List<string>() { "DiseaseBloodFirst", "DiseaseBloodSecond", "DiseaseBloodThird" });
        component.Symptoms.Add("Headache", (1, 4));
    }

    private void OnMapInit(EntityUid uid, DiseaseRoleComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, DiseaseShopId);
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
                    bool f = _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                    {
                        {diseaseComp.CurrencyPrototype, -value}
                    }, uid);
                    _store.UpdateUserInterface(uid, uid, store);
                    return true;
                } else
                {
                    return false;
                }
            }
        }
        return false;
    }
    
    private void OnCough(EntityUid uid, DiseaseRoleComponent component, DiseaseStartCoughEvent args)
    {
        component.Symptoms.Add("Cough", (2, 9999));
    }

    private void OnSneeze(EntityUid uid, DiseaseRoleComponent component, DiseaseStartSneezeEvent args)
    {
        //component.Sneeze = true;
        component.Symptoms.Add("Sneeze", (3, 9999));
    }

    private void OnVomit(EntityUid uid, DiseaseRoleComponent component, DiseaseStartVomitEvent args)
    {
        component.Symptoms.Add("Vomit", (3, 9999));
    }

    private void OnCrying(EntityUid uid, DiseaseRoleComponent component, DiseaseStartCryingEvent args)
    {
        component.Symptoms.Add("Crying", (0, 9999));
    }

    private void OnNarcolepsy(EntityUid uid, DiseaseRoleComponent component, DiseaseNarcolepsyEvent args)
    {
        component.Symptoms.Add("Narcolepsy", (3, 9999));
    }

    private void OnMuted(EntityUid uid, DiseaseRoleComponent component, DiseaseMutedEvent args)
    {
        component.Symptoms.Add("Muted", (4, 9999));
    }

    private void OnSlowness(EntityUid uid, DiseaseRoleComponent component, DiseaseSlownessEvent args)
    {
        component.Symptoms.Add("Slowness", (2, 9999));
    }
    private void OnBleed(EntityUid uid, DiseaseRoleComponent component, DiseaseBleedEvent args)
    {
        component.Symptoms.Add("Bleed", (3, 9999));
    }
    private void OnBlindness(EntityUid uid, DiseaseRoleComponent component, DiseaseBlindnessEvent args)
    {
        component.Symptoms.Add("Blindness", (4, 9999));
    }
    private void OnInsult(EntityUid uid, DiseaseRoleComponent component, DiseaseInsultEvent args)
    {
        component.Symptoms.Add("Insult", (2, 9999));
    }



    private void OnZombie(EntityUid uid, DiseaseRoleComponent component, DiseaseZombieEvent args)
    {
        var inf = component.Infected.ToArray();
        foreach(EntityUid infected in inf)
        {
            if (_random.Prob(0.8f)) {
                RemComp<SickComponent>(infected);
                component.Infected.Remove(infected);
                EnsureComp<ZombifyOnDeathComponent>(infected);
                EnsureComp<PendingZombieComponent>(infected);
            }
        }
    }

}
