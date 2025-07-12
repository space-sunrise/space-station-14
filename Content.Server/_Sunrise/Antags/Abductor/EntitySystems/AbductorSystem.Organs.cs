using Content.Shared._Sunrise.Antags.Abductor;
using Content.Shared._Sunrise.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Server.GameObjects;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Popups;
using System;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Robust.Shared.Toolshed.TypeParsers;
using Content.Shared.Damage.Prototypes;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Jittering;

namespace Content.Server._Sunrise.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SolutionContainerSystem _type = default!;


    private float _delayAccumulator = 0f;
    private readonly Stopwatch _stopwatch = new();
    private readonly DamageSpecifier _passiveHealing = new();

    public void InitializeOrgans()
    {
        foreach (var specif in _prototypes.EnumeratePrototypes<DamageTypePrototype>())
            _passiveHealing.DamageDict.Add(specif.ID, -1);
        _stopwatch.Start();
    }

    public override void Update(float frameTime)
    {
        _delayAccumulator += frameTime;
        if (_delayAccumulator > 3)
        {
            _delayAccumulator = 0;
            _stopwatch.Restart();
            var query = EntityQueryEnumerator<AbductorVictimComponent>();
            while (query.MoveNext(out var uid, out var victim) && _stopwatch.Elapsed < TimeSpan.FromMilliseconds(0.5))
            {
                if(victim.Organ != AbductorOrganType.None)
                    Do(uid, victim);
            }
        }
    }

    private void Do(EntityUid uid, AbductorVictimComponent victim)
    {
        switch (victim.Organ)
        {
            case AbductorOrganType.Health:
                if(_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(3))
                    return;
                victim.LastActivation = _time.CurTime;
                _damageable.TryChangeDamage(uid, _passiveHealing);
                break;
            case AbductorOrganType.Gravity:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;
                victim.LastActivation = _time.CurTime;
                var gravity = SpawnAttachedTo("AbductorGravityGlandGravityWell", Transform(uid).Coordinates);
                _xformSys.SetParent(gravity, uid);
                break;
            case AbductorOrganType.Egg:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;
                victim.LastActivation = _time.CurTime;
                SpawnAttachedTo("FoodEggChickenFertilized", Transform(uid).Coordinates);
                break;
            case AbductorOrganType.Spider:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(240))
                    return;
                victim.LastActivation = _time.CurTime;
                SpawnAttachedTo("EggSpiderFertilized", Transform(uid).Coordinates);
                break;
            case AbductorOrganType.Ephedrine:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;
                victim.LastActivation = _time.CurTime;
                TryComp<SolutionContainerManagerComponent>(uid, out var solution);
                if (_solutions.TryGetInjectableSolution(uid, out var injectable, out _))
                {
                    _solutions.TryAddReagent(injectable.Value, "Ephedrine", 5f);
                }
                break;
            default:
                break;
        }
    }
}
