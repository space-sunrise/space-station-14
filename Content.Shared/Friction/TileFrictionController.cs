using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Gravity;
// using Content.Shared.Interaction.Components; //sunrise
using Content.Shared.Interaction.Events;
// using Content.Shared.Movement.Components; //sunrise
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
// using Robust.Shared.Utility; //sunrise
// using Content.Shared.Mech.Components; //sunrise-edit //sunrise
using Robust.Shared.Maths; //sunrise

namespace Content.Shared.Friction
{
    public sealed partial class TileFrictionController : VirtualController //sunrise-edit
    {
	//sunrise-edit-start
        [Dependency] private IConfigurationManager _configManager = default!;
        [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private SharedGravitySystem _gravity = default!;
        [Dependency] private SharedMoverController _mover = default!;
        [Dependency] private SharedMapSystem _map = default!;
	//sunrise-edit-end

        private EntityQuery<TileFrictionModifierComponent> _frictionQuery;
        private EntityQuery<TransformComponent> _xformQuery; //sunrise-edit
        private EntityQuery<PullerComponent> _pullerQuery;
        private EntityQuery<PullableComponent> _pullableQuery;
        private EntityQuery<MapGridComponent> _gridQuery;
/* //sunrise-start
        private EntityQuery<MechComponent> _mechQuery; //sunrise-edit

        // For debug purposes only
        private EntityQuery<InputMoverComponent> _moverQuery;
        private EntityQuery<BlockMovementComponent> _blockMoverQuery;
*/ //sunrise-end

        private float _frictionModifier;
        private float _minDamping;
        private float _airDamping;
        private float _offGridDamping;
	//sunrise-start
        /// <summary>Per physics BeforeSolve pass; tile friction for the same cell is reused for all awake bodies.</summary>
        private int _tileFrictionCacheSerial;
        private readonly Dictionary<(EntityUid Grid, Vector2i Cell), (int Serial, float Friction)> _tileFrictionCache = new();
	//sunrise-end
        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configManager, CCVars.TileFrictionModifier, value => _frictionModifier = value, true);
            Subs.CVar(_configManager, CCVars.MinFriction, value => _minDamping = value, true);
            Subs.CVar(_configManager, CCVars.AirFriction, value => _airDamping = value, true);
            Subs.CVar(_configManager, CCVars.OffgridFriction, value => _offGridDamping = value, true);
            _frictionQuery = GetEntityQuery<TileFrictionModifierComponent>(); //sunrise
            _xformQuery = GetEntityQuery<TransformComponent>();
            _pullerQuery = GetEntityQuery<PullerComponent>();
            _pullableQuery = GetEntityQuery<PullableComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
/* //sunrise-start
            _moverQuery = GetEntityQuery<InputMoverComponent>();
            _blockMoverQuery = GetEntityQuery<BlockMovementComponent>();
            _mechQuery = GetEntityQuery<MechComponent>(); //sunrise-edit
*/ //sunrise-end
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
		//sunrise-start
            unchecked
            {
                _tileFrictionCacheSerial++;
            }

            if (_tileFrictionCacheSerial == 0)
                _tileFrictionCacheSerial = 1;

            if (_tileFrictionCache.Count > 8192)
                _tileFrictionCache.Clear();
		//sunrise-end
            foreach (var ent in PhysicsSystem.AwakeBodies)
            {
                var uid = ent.Owner;
                var body = ent.Comp1;

                // Only apply friction when it's not a mob (or the mob doesn't have control)
                // We may want to instead only apply friction to dynamic entities and not mobs ever.
//                if (_mechQuery.HasComp(uid)) //sunrise-edit //sunrise-edit
//                    continue; //sunrise-edit //sunrise-edit

                if (prediction && !body.Predict || _mover.UseMobMovement(uid))
                    continue;

                if (body.LinearVelocity.Equals(Vector2.Zero) && body.AngularVelocity.Equals(0f))
                    continue;

                var xform = ent.Comp2;
                float friction;

                // If we're not touching the ground, don't use tileFriction.
                // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
                if (body.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(uid) || !xform.Coordinates.IsValid(EntityManager))
                    friction = xform.GridUid == null || !_gridQuery.HasComp(xform.GridUid) ? _offGridDamping : _airDamping;
                else
                    friction = _frictionModifier * GetTileFriction(uid, body, xform);

                var bodyModifier = 1f;

                if (_frictionQuery.TryGetComponent(uid, out var frictionComp))
                {
                    bodyModifier = frictionComp.Modifier;
                }

                var ev = new TileFrictionEvent(bodyModifier);

                RaiseLocalEvent(uid, ref ev);
                bodyModifier = ev.Modifier;

                // If we're sandwiched between 2 pullers reduce friction
                // Might be better to make this dynamic and check how many are in the pull chain?
                // Either way should be much faster for now.
                if (_pullerQuery.TryGetComponent(uid, out var puller) && puller.Pulling != null &&
                    _pullableQuery.TryGetComponent(uid, out var pullable) && pullable.BeingPulled)
                {
                    bodyModifier *= 0.2f;
                }

                friction *= bodyModifier;

                friction = Math.Max(_minDamping, friction);

                PhysicsSystem.SetLinearDamping(uid, body, friction);
                PhysicsSystem.SetAngularDamping(uid, body, friction);

                if (body.BodyType != BodyType.KinematicController)
//                { //sunrise-edit
                    /*
                     * Extra catch for input movers that may be temporarily unable to move for whatever reason.
                     * Block movement shouldn't be added and removed frivolously so it should be reliable to use this
                     * as a check for brains and such which have input mover purely for ghosting behavior.
                     */
//                    DebugTools.Assert(!_moverQuery.HasComp(uid) || _blockMoverQuery.HasComp(uid), //sunrise-edit
//                        $"Input mover: {ToPrettyString(uid)} in TileFrictionController is not the correct BodyType, BodyType found: {body.BodyType}, expected: KinematicController."); //sunrise-edit
                    continue;
//				} //sunrise-edit

                // Physics engine doesn't apply damping to Kinematic Controllers so we have to do it here.
                // BEWARE YE TRAVELLER:
                // You may think you can just pass the body.LinearVelocity to the Friction function and edit it there!
                // But doing so is unpredicted! And you will doom yourself to 1000 years of rubber banding!
                var velocity = body.LinearVelocity;
                var angVelocity = body.AngularVelocity;
                _mover.Friction(0f, frameTime, friction, ref velocity);
                _mover.Friction(0f, frameTime, friction, ref angVelocity);
                PhysicsSystem.SetLinearVelocity(uid, velocity, body: body);
                PhysicsSystem.SetAngularVelocity(uid, angVelocity, body: body);
            }
        }

        [Pure]
        private float GetTileFriction(
            EntityUid uid,
            PhysicsComponent body,
            TransformComponent xform)
        {
            var tileModifier = 1f;
            // If not on a grid and not in the air then return the map's friction.
            if (!_gridQuery.TryGetComponent(xform.GridUid, out var grid))
            {
                return _frictionQuery.TryGetComponent(xform.MapUid, out var friction)
                    ? friction.Modifier
                    : tileModifier;
            }

            var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
//sunrise-start
            var cacheKey = (xform.GridUid.Value, tile.GridIndices);

            if (_tileFrictionCache.TryGetValue(cacheKey, out var cached) && cached.Serial == _tileFrictionCacheSerial)
                return cached.Friction; 
//sunrise-end

            // If it's a map but on an empty tile then just assume it has gravity.
            if (tile.Tile.IsEmpty &&
                HasComp<MapComponent>(xform.GridUid) &&
                (!TryComp<GravityComponent>(xform.GridUid, out var gravity) || gravity.Enabled))
            { //sunrise-edit
                _tileFrictionCache[cacheKey] = (_tileFrictionCacheSerial, tileModifier); //sunrise-edit
                return tileModifier;
            } //sunrise-edit

            // Check for anchored ents that modify friction
            var anc = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, grid, tile.GridIndices);
            while (anc.MoveNext(out var tileEnt))
            {
                if (_frictionQuery.TryGetComponent(tileEnt, out var friction))
                    tileModifier *= friction.Modifier;
            }

            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
//sunrise-start
//            return tileDef.Friction * tileModifier;
            var result = tileDef.Friction * tileModifier;
            _tileFrictionCache[cacheKey] = (_tileFrictionCacheSerial, result);
            return result;
//sunrise-end
        }

        public void SetModifier(EntityUid entityUid, float value, TileFrictionModifierComponent? friction = null)
        {
            if (!Resolve(entityUid, ref friction) || value.Equals(friction.Modifier))
                return;

            friction.Modifier = value;
            Dirty(entityUid, friction);
        }
    }
}