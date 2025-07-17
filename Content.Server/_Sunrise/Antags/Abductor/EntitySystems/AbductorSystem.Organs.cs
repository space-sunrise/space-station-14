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
using Robust.Shared.Toolshed.Commands.GameTiming;
using Content.Server.Speech.Components;
using Content.Server.Humanoid;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Discord.Rest;
using Content.Shared._Sunrise.Felinid;

namespace Content.Server._Sunrise.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;


    private float _delayAccumulator = 0f;
    private readonly Stopwatch _stopwatch = new();
    private readonly DamageSpecifier _passiveHealing = new();

    public void InitializeOrgans()
    {
        foreach (var specif in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
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
                if (victim.Organ != AbductorOrganType.None)
                    Do(uid, victim);
            }
        }
    }

    private void Do(EntityUid uid, AbductorVictimComponent victim)
    {
        if (HasComp<AbductorOwoTransformatedComponent>(uid))
            return;

        var curTime = _time.CurTime;

        switch (victim.Organ)
        {
            case AbductorOrganType.Health:

                if (curTime - victim.LastActivation < TimeSpan.FromSeconds(3))
                    return;

                victim.LastActivation = curTime;
                _damageable.TryChangeDamage(uid, _passiveHealing);
                break;

            case AbductorOrganType.Gravity:

                if (curTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;

                victim.LastActivation = curTime;
                var gravity = SpawnAttachedTo("AbductorGravityGlandGravityWell", Transform(uid).Coordinates);
                _xformSys.SetParent(gravity, uid);
                break;

            case AbductorOrganType.Egg:

                if (curTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;

                victim.LastActivation = curTime;
                SpawnAttachedTo("FoodEggChickenFertilized", Transform(uid).Coordinates);
                break;

            case AbductorOrganType.Spider:

                if (curTime - victim.LastActivation < TimeSpan.FromSeconds(240))
                    return;

                victim.LastActivation = curTime;
                SpawnAttachedTo("EggSpiderFertilized", Transform(uid).Coordinates);
                break;

            case AbductorOrganType.Ephedrine:

                if (curTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;

                victim.LastActivation = curTime;
                TryComp<SolutionContainerManagerComponent>(uid, out var solution);

                if (_solutions.TryGetInjectableSolution(uid, out var injectable, out _))
                    _solutions.TryAddReagent(injectable.Value, "Ephedrine", 5f);
                break;

            case AbductorOrganType.Owo:
                if (curTime > victim.TransformationTime)
                {
                    if (HasComp<FelinidComponent>(uid))
                        return;
                    EnsureComp<AbductorOwoTransformatedComponent>(uid);
                    _popup.PopupEntity(Loc.GetString("owo-organ-transformation"), uid, PopupType.LargeCaution);
                    _audioSystem.PlayPvs(victim.Mew, uid);
                    SpawnAttachedTo("RMCExplosionEffectGrenadeShockWave", uid.ToCoordinates());
                    EnsureComp<OwOAccentComponent>(uid);
                    _humanoid.AddMarking(uid, "CatTail");
                    _humanoid.AddMarking(uid, "CatEars"); // На ласте еще добавлять костюм горничной и лапки, why not
                }

                break;

            case AbductorOrganType.EMP:

                if (curTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;

                victim.LastActivation = curTime;
                SpawnAttachedTo("AdminInstantEffectEMP", Transform(uid).Coordinates);
                break;


            default:
                break;
        }
    }
}
