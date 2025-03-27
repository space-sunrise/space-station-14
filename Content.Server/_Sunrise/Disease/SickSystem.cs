// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Sunrise.Disease;
using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Humanoid;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Chat;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Server.Medical;
using Content.Server.Traits.Assorted;
using Content.Shared.Traits.Assorted;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Item;
using Content.Shared.Speech.Muting;
using Content.Shared.Store.Components;
namespace Content.Server._Sunrise.Disease;
public sealed class SickSystem : SharedSickSystem
{
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IServerEntityManager _entityManager = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    private EntityLookupSystem Lookup => _entityManager.System<EntityLookupSystem>();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SickComponent, ComponentShutdown>(OnShut);
        SubscribeLocalEvent<SickComponent, EmoteEvent>(OnEmote, before:
        new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
    }

    public void OnShut(EntityUid uid, SickComponent component, ComponentShutdown args)
    {
        if (!Exists(uid))
            return;

        if (TryComp<AutoEmoteComponent>(uid, out var autoEmoteComponent))
        {
            foreach (var emote in autoEmoteComponent.Emotes)
            {
                if (emote.Contains("Infected"))
                {
                    _autoEmote.RemoveEmote(uid, emote);
                }
            }
        }

        foreach (var key in component.Symptoms)
        {
            switch (key)
            {
                case "Narcolepsy":
                    if (HasComp<SleepyComponent>(uid))
                        RemComp<SleepyComponent>(uid);
                    break;
                case "Muted":
                    if (HasComp<MutedComponent>(uid))
                        RemComp<MutedComponent>(uid);
                    break;
                case "Blindness":
                    if (HasComp<PermanentBlindnessComponent>(uid))
                        RemComp<PermanentBlindnessComponent>(uid);
                    if (HasComp<BlurryVisionComponent>(uid))
                        RemComp<BlurryVisionComponent>(uid);
                    if (HasComp<EyeClosingComponent>(uid))
                        RemComp<EyeClosingComponent>(uid);
                    break;
                case "Slowness":
                    if (HasComp<SpeedModifierOnComponent>(uid))
                        RemComp<SpeedModifierOnComponent>(uid);
                    break;
                case "Bleed":
                    if (HasComp<MinimumBleedComponent>(uid))
                        RemComp<MinimumBleedComponent>(uid);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(component.BeforeInfectedBloodReagent) && 
            TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            _bloodstream.ChangeBloodReagent(uid, component.BeforeInfectedBloodReagent);
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SickComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (Terminating(uid))
                continue;

            if (TryComp<DiseaseRoleComponent>(component.owner, out var diseaseComp))
            {
                UpdateInfection(uid, component, component.owner, diseaseComp);
                if (!component.Inited)
                {
                    //Infect
                    if (TryComp<BloodstreamComponent>(uid, out var stream))
                        component.BeforeInfectedBloodReagent = stream.BloodReagent;
                    _bloodstream.ChangeBloodReagent(uid, diseaseComp.NewBloodReagent);

                    RaiseNetworkEvent(new ClientInfectEvent(GetNetEntity(uid), GetNetEntity(component.owner)));
                    diseaseComp.SickOfAllTime++;
                    AddMoney(uid, 5);

                    component.Inited = true;
                }
                else
                {
                    if (_gameTiming.CurTime >= component.NextStadyAt)
                    {
                        component.Stady++;
                        foreach (var emote in EnsureComp<AutoEmoteComponent>(uid).Emotes)
                        {
                            if (emote.Contains("Infected"))
                            {
                                _autoEmote.RemoveEmote(uid, emote);
                            }
                        }
                        component.Symptoms.Clear();
                        component.NextStadyAt = _gameTiming.CurTime + component.StadyDelay;
                    }
                }
            }
        }
    }

    void AddMoney(EntityUid uid, FixedPoint2 value)
    {
        if (TryComp<SickComponent>(uid, out var component))
        {
            if (TryComp<DiseaseRoleComponent>(component.owner, out var diseaseComp))
            {
                if (TryComp<StoreComponent>(component.owner, out var store))
                {
                    bool f = _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                    {
                        {diseaseComp.CurrencyPrototype, value}
                    }, component.owner);
                    _store.UpdateUserInterface(component.owner, component.owner, store);
                }
            }
        }
    }

    private void UpdateInfection(EntityUid uid, SickComponent component, EntityUid disease, DiseaseRoleComponent diseaseComponent)
    {
        foreach ((var key, (var min, var max)) in diseaseComponent.Symptoms)
        {
            if (!component.Symptoms.Contains(key))
            {
                if (component.Stady >= min && component.Stady <= max)
                {
                    component.Symptoms.Add(key);
                    EnsureComp<AutoEmoteComponent>(uid);
                    switch (key)
                    {
                        case "Headache":
                            _autoEmote.AddEmote(uid, "InfectedHeadache");
                            break;
                        case "Cough":
                            _autoEmote.AddEmote(uid, "InfectedCough");
                            break;
                        case "Sneeze":
                            _autoEmote.AddEmote(uid, "InfectedSneeze");
                            break;
                        case "Vomit":
                            _autoEmote.AddEmote(uid, "InfectedVomit");
                            break;
                        case "Crying":
                            _autoEmote.AddEmote(uid, "InfectedCrying");
                            break;
                        case "Narcolepsy":
                            if (!HasComp<SleepyComponent>(uid))
                            {
                                var c = AddComp<SleepyComponent>(uid);
                                EntityManager.EntitySysManager.GetEntitySystem<SleepySystem>().SetNarcolepsy(uid, new Vector2(10, 30), new Vector2(300, 600), c);
                            }
                            break;
                        case "Muted":
                            EnsureComp<MutedComponent>(uid);
                            break;
                        case "Blindness":
                            EnsureComp<PermanentBlindnessComponent>(uid);
                            break;
                        case "Slowness":
                            EnsureComp<SpeedModifierOnComponent>(uid);
                            break;
                        case "Bleed":
                            EnsureComp<MinimumBleedComponent>(uid);
                            break;
                        case "Insult":
                            _autoEmote.AddEmote(uid, "InfectedInsult");
                            break;
                    }
                }
            }
        }
    }

    private void OnEmote(EntityUid uid, SickComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        if (!component.Symptoms.Contains(args.Emote.ID)) return;
        switch (args.Emote.ID)
        {
            case "Headache":
                _popupSystem.PopupEntity(Loc.GetString("disease-symptom-headache"), uid, uid, PopupType.Small);
                break;
            case "Cough":
                if (_robustRandom.Prob(0.9f))
                {
                    if (TryComp<DiseaseRoleComponent>(component.owner, out var disease))
                    {
                        if (_prototypeManager.TryIndex<DamageTypePrototype>("Piercing", out var damagePrototype))
                        {
                            _damageableSystem.TryChangeDamage(uid, new(damagePrototype, 0.25f * disease.Lethal), true, origin: uid);
                        }

                        foreach (var entity in Lookup.GetEntitiesInRange(uid, 0.7f))
                        {
                            if (_robustRandom.Prob(disease.CoughInfectChance))
                            {
                                if (HasComp<HumanoidAppearanceComponent>(entity) && !HasComp<SickComponent>(entity) && !HasComp<DiseaseImmuneComponent>(entity))
                                {
                                    OnInfected(entity, component.owner, Comp<DiseaseRoleComponent>(component.owner).CoughInfectChance);
                                }
                            }
                        }
                    }
                }
                break;
            case "Sneeze":
                if (_robustRandom.Prob(0.9f))
                {
                    if (TryComp<DiseaseRoleComponent>(component.owner, out var disease))
                    {
                        foreach (var entity in Lookup.GetEntitiesInRange(uid, 1.2f))
                        {
                            if (_robustRandom.Prob(disease.CoughInfectChance))
                            {
                                if (HasComp<HumanoidAppearanceComponent>(entity) && !HasComp<SickComponent>(entity) && !HasComp<DiseaseImmuneComponent>(entity))
                                {
                                    OnInfected(entity, component.owner, Comp<DiseaseRoleComponent>(component.owner).CoughInfectChance);
                                }
                            }
                        }
                    }
                }
                break;
            case "Vomit":
                if (_robustRandom.Prob(0.4f))
                {
                    _vomitSystem.Vomit(uid, -30, -20);
                }
                break;
            case "Insult":
                if (TryComp<DiseaseRoleComponent>(component.owner, out var dis))
                {
                    _stun.TryParalyze(uid, TimeSpan.FromSeconds(5), false);
                    if (_prototypeManager.TryIndex<DamageTypePrototype>("Shock", out var damagePrototype))
                    {
                        _damageableSystem.TryChangeDamage(uid, new(damagePrototype, 0.35f * dis.Lethal), true, origin: uid);
                    }
                }
                break;
        }
    }
}
