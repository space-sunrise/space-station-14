using System.Linq;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.FleshCult
{
    public sealed class FleshPudgeSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly GunSystem _gunSystem = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FleshPudgeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<FleshPudgeComponent, FleshPudgeThrowFaceHuggerActionEvent>(OnThrowFaceHugger);
            SubscribeLocalEvent<FleshPudgeComponent, FleshPudgeAbsorbBloodPoolActionEvent>(OnAbsorbBloodPoolActionEvent);
            SubscribeLocalEvent<FleshPudgeComponent, FleshPudgeAcidSpitActionEvent>(OnAcidSpit);
        }

        private void OnThrowFaceHugger(EntityUid uid, FleshPudgeComponent component, FleshPudgeThrowFaceHuggerActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var worm = Spawn(component.FaceHuggerMobSpawnId, Transform(uid).Coordinates);
            var xform = Transform(uid);
            var mapCoords = args.Target.ToMap(_entityManager, _transformSystem);
            var direction = mapCoords.Position - xform.MapPosition.Position;

            _throwing.TryThrow(worm, direction, 7F, uid, 10F);
            if (component.SoundThrowWorm != null)
            {
                _audioSystem.PlayPvs(component.SoundThrowWorm, uid, component.SoundThrowWorm.Params);
            }
            _popupSystem.PopupEntity(Loc.GetString("flesh-pudge-throw-hugger-popup"), uid, PopupType.LargeCaution);
        }

        private void OnAcidSpit(EntityUid uid, FleshPudgeComponent component, FleshPudgeAcidSpitActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var acidBullet = Spawn(component.BulletAcidSpawnId, Transform(uid).Coordinates);
            var xform = Transform(uid);
            var mapCoords = args.Target.ToMap(_entityManager, _transformSystem);
            var direction = mapCoords.Position - xform.MapPosition.Position;
            var userVelocity = _physics.GetMapLinearVelocity(uid);

            _gunSystem.ShootProjectile(acidBullet, direction, userVelocity, uid, uid);
            _audioSystem.PlayPvs(component.BloodAbsorbSound, uid, component.BloodAbsorbSound.Params);
        }

        private void OnAbsorbBloodPoolActionEvent(EntityUid uid, FleshPudgeComponent component,
            FleshPudgeAbsorbBloodPoolActionEvent args)
        {
            if (args.Handled)
                return;

            var xform = Transform(uid);
            var puddles = new ValueList<(EntityUid Entity, string Solution)>();
            puddles.Clear();
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, 1f))
            {
                if (TryComp<PuddleComponent>(entity, out var puddle))
                {
                    puddles.Add((entity, puddle.SolutionName));
                }
            }

            if (puddles.Count == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-not-find-puddles"),
                    uid, uid, PopupType.Large);
                return;
            }

            var absorbBlood = new Solution();
            foreach (var (puddle, solution) in puddles)
            {
                if (!_solutionSystem.TryGetSolution(puddle, solution, out var puddleSolution))
                {
                    continue;
                }
                foreach (var puddleSolutionContent in puddleSolution.Value.Comp.Solution.ToList())
                {
                    if (!component.BloodWhitelist.Contains(puddleSolutionContent.Reagent.Prototype))
                        continue;

                    var blood = puddleSolution.Value.Comp.Solution.SplitSolutionWithOnly(
                        puddleSolutionContent.Quantity, puddleSolutionContent.Reagent.Prototype);

                    absorbBlood.AddSolution(blood, _prototypeManager);
                }

                var ev = new SolutionContainerChangedEvent(puddleSolution.Value.Comp.Solution, solution);
                RaiseLocalEvent(puddle, ref ev);
            }

            if (absorbBlood.Volume == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-cant-absorb-puddle"),
                    uid, uid, PopupType.Large);
                return;
            }

            _audioSystem.PlayPvs(component.BloodAbsorbSound, uid, component.BloodAbsorbSound.Params);
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-absorb-puddle", ("Entity", uid)),
                uid, uid, PopupType.Large);

            var transferSolution = new Solution();
            foreach (var solution in component.HealBloodAbsorbReagents)
            {
                transferSolution.AddReagent(solution.Reagent, solution.Quantity * (absorbBlood.Volume / 10));
            }

            if (_solutionSystem.TryGetInjectableSolution(uid, out var injectableSolution, out var _))
            {
                _solutionSystem.TryAddSolution(injectableSolution.Value, transferSolution);
            }
            absorbBlood.RemoveAllSolution();
            args.Handled = true;
        }

        private void OnStartup(EntityUid uid, FleshPudgeComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionAcidSpitId);
            _action.AddAction(uid, component.ActionThrowWormId);
            _action.AddAction(uid, component.ActionAbsorbBloodPoolId);
        }
    }
}
