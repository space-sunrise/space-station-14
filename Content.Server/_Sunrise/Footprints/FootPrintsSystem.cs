using Content.Server.Atmos.Components;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Content.Shared.Standing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Footprints;

/// <summary>
/// Handles creation and management of footprints left by entities as they move.
/// </summary>
public sealed class FootprintSystem : EntitySystem
{
    #region Dependencies
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedStandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    #endregion

    #region Entity Queries
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    #endregion

    public static readonly float FootsVolume = 5;
    public static readonly float BodySurfaceVolume = 15;

    // Dictionary to track footprints per tile to prevent overcrowding
    private const int MaxFootprintsPerTile = 6;
    private const int MaxMarksPerTile = 3;

    #region Initialization
    /// <summary>
    /// Initializes the footprint system and sets up required queries and subscriptions.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<FootprintEmitterComponent, ComponentStartup>(OnEmitterStartup);
        SubscribeLocalEvent<FootprintEmitterComponent, MoveEvent>(OnEntityMove);
        SubscribeLocalEvent<FootprintEmitterComponent, ComponentInit>(OnFootprintEmitterInit);
    }

    private void OnFootprintEmitterInit(Entity<FootprintEmitterComponent> entity, ref ComponentInit args)
    {
        _solutionSystem.EnsureSolution(entity.Owner, entity.Comp.FootsSolutionName, out _, FixedPoint2.New(FootsVolume));
        _solutionSystem.EnsureSolution(entity.Owner, entity.Comp.BodySurfaceSolutionName, out _, FixedPoint2.New(BodySurfaceVolume));
    }

    /// <summary>
    /// Handles initialization of footprint emitter components.
    /// </summary>
    private void OnEmitterStartup(EntityUid uid, FootprintEmitterComponent component, ComponentStartup args)
    {
        // Add small random variation to step interval
        component.WalkStepInterval = Math.Max(0f, component.WalkStepInterval + _random.NextFloat(-0.05f, 0.05f));
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// Handles entity movement and creates footprints when appropriate.
    /// </summary>
    private void OnEntityMove(EntityUid uid, FootprintEmitterComponent emitter, ref MoveEvent args)
    {
        // Check if footprints should be created
        if (!_transformQuery.TryComp(uid, out var transform)
            || !_physicsQuery.TryGetComponent(uid, out var body)
            || body.BodyStatus == BodyStatus.InAir
            || _gravity.IsWeightless(uid)
            || !_mapManager.TryFindGridAt(_transformSystem.GetMapCoordinates((uid, transform)), out var gridUid, out var grid)
            || !TryComp<SolutionContainerManagerComponent>(uid, out var container))
            return;

        var stand = !_standingStateSystem.IsDown(uid);

        var solCont = (uid, container);
        Solution solution;
        Entity<SolutionComponent> solComp;

        if (stand)
        {
            if (!_solutionSystem.ResolveSolution(solCont, emitter.FootsSolutionName, ref emitter.FootsSolution, out var footsSolution))
                return;

            solution = footsSolution;
            solComp = emitter.FootsSolution.Value;
        }
        else
        {
            if (!_solutionSystem.ResolveSolution(solCont, emitter.BodySurfaceSolutionName, ref emitter.BodySurfaceSolution, out var bodySurfaceSolution))
                return;

            solution = bodySurfaceSolution;
            solComp = emitter.BodySurfaceSolution.Value;
        }

        if (solution.Volume <= 0)
            return;

        var distanceMoved = (transform.LocalPosition - emitter.LastStepPosition).Length();
        var requiredDistance = stand ? emitter.WalkStepInterval : emitter.DragMarkInterval;

        if (!(distanceMoved > requiredDistance))
            return;

        var tileRef = _mapSystem.GetTileRef((gridUid, grid), transform.Coordinates);


        var footPrints = new HashSet<Entity<FootprintComponent>>();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tileRef.GridIndices, footPrints);
        var dragMarkCount = 0;
        var footPrintCount = 0;
        foreach (var footPrint in footPrints)
        {
            switch (footPrint.Comp.PrintType)
            {
                case PrintType.Foot:
                    footPrintCount += 1;
                    break;
                case PrintType.DragMark:
                    dragMarkCount += 1;
                    break;
            }
        }
        if (stand)
        {
            if (footPrintCount >= MaxFootprintsPerTile)
                return;
        }
        else
        {
            if (dragMarkCount >= MaxMarksPerTile)
                return;
        }

        emitter.IsRightStep = !emitter.IsRightStep;

        // Create new footprint entity
        var footprintEntity = SpawnFootprint(gridUid, emitter, solution, uid, transform, stand);

        // Update footprint and emitter state
        UpdateFootprint(footprintEntity, (uid, emitter), solComp, transform, stand);

        // Update emitter state.
        UpdateEmitterState(emitter, transform);
    }

    #endregion

    #region Footprint Creation and Management
    /// <summary>
    /// Creates a new footprint entity at the calculated position.
    /// </summary>
    private EntityUid SpawnFootprint(
        EntityUid gridUid,
        FootprintEmitterComponent emitter,
        Solution emitterSolution,
        EntityUid emitterOwner,
        TransformComponent transform,
        bool stand)
    {
        var coords = CalculateFootprintPosition(gridUid, emitter, transform, stand);
        var entity = Spawn(stand ? emitter.FootprintPrototype : emitter.DragMarkPrototype, coords);

        if (_appearanceQuery.TryComp(entity, out var appearance))
        {
            var visualType = DetermineVisualState(emitterOwner, stand);
            _appearanceSystem.SetData(entity,
                FootprintVisualParameter.VisualState,
                GetStateId(visualType, emitter),
                appearance);

            var rawAlpha = emitterSolution.Volume.Float() / emitterSolution.MaxVolume.Float();
            var alpha = Math.Clamp((0.8f * rawAlpha) + 0.3f, 0.5f, 1f);

            _appearanceSystem.SetData(entity,
                FootprintVisualParameter.TrackColor,
                emitterSolution.GetColor(_prototypeManager).WithAlpha(alpha),
                appearance);
        }

        return entity;
    }

    private string GetStateId(FootprintVisualType visualType, FootprintEmitterComponent emitter)
    {
        return visualType switch
        {
            FootprintVisualType.BareFootprint => emitter.IsRightStep
                ? _random.Pick(emitter.RightBareFootState)
                : _random.Pick(emitter.LeftBareFootState),
            FootprintVisualType.ShoeFootprint => _random.Pick(emitter.ShoeFootState),
            FootprintVisualType.SuitFootprint => _random.Pick(emitter.PressureSuitFootState),
            FootprintVisualType.DragMark => _random.Pick(emitter.DraggingStates),
            _ => throw new ArgumentOutOfRangeException(
                $"Unknown footprint visual type: {visualType}")
        };
    }

    /// <summary>
    /// Updates footprint rotation and reagent transfer.
    /// </summary>
    private void UpdateFootprint(
        EntityUid footprintEntity,
        Entity<FootprintEmitterComponent> emitter,
        Entity<SolutionComponent> emitterEntSolution,
        TransformComponent transform,
        bool stand)
    {
        if (!_transformQuery.TryComp(footprintEntity, out var footprintTransform))
            return;

        footprintTransform.LocalRotation = stand
            ? transform.LocalRotation + Angle.FromDegrees(180f)
            : (transform.LocalPosition - emitter.Comp.LastStepPosition).ToAngle() + Angle.FromDegrees(-90f);

        TransferReagents(footprintEntity, emitter, emitterEntSolution, stand);
    }
    #endregion

    #region State Management
    /// <summary>
    /// Updates emitter state after creating a footprint.
    /// </summary>
    private void UpdateEmitterState(FootprintEmitterComponent emitter, TransformComponent transform)
    {
        emitter.LastStepPosition = transform.LocalPosition;
    }

    /// <summary>
    /// Transfers reagents from emitter to footprint if applicable.
    /// </summary>
    private void TransferReagents(EntityUid footprintEntity, Entity<FootprintEmitterComponent> emitter, Entity<SolutionComponent> emitterSolution, bool stand)
    {
        if (!TryComp<SolutionContainerManagerComponent>(footprintEntity, out var container)
            || !TryComp<FootprintComponent>(footprintEntity, out var footprint)
            || !_solutionSystem.ResolveSolution((footprintEntity, container),
                footprint.ContainerName,
                ref footprint.SolutionContainer,
                out var solution))
            return;

        var splitSolution = _solutionSystem.SplitSolution(emitterSolution, stand ? emitter.Comp.TransferVolumeFoot : emitter.Comp.TransferVolumeDragMark);

        _solutionSystem.AddSolution(footprint.SolutionContainer.Value, splitSolution);
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Calculates the position where a footprint should be placed.
    /// </summary>
    private EntityCoordinates CalculateFootprintPosition(
        EntityUid uid,
        FootprintEmitterComponent emitter,
        TransformComponent transform,
        bool stand)
    {
        if (!stand)
            return new EntityCoordinates(uid, transform.LocalPosition);

        var offset = emitter.IsRightStep
            ? new Angle(Angle.FromDegrees(180f) + transform.LocalRotation)
                .RotateVec(emitter.PlacementOffset)
            : new Angle(transform.LocalRotation).RotateVec(emitter.PlacementOffset);

        return new EntityCoordinates(uid, transform.LocalPosition + offset);

    }

    /// <summary>
    /// Determines the visual state for a footprint based on entity equipment.
    /// </summary>
    private FootprintVisualType DetermineVisualState(EntityUid uid, bool stand)
    {
        if (!stand)
            return FootprintVisualType.DragMark;

        var state = FootprintVisualType.BareFootprint;

        if (_inventory.TryGetSlotEntity(uid, "shoes", out _))
            state = FootprintVisualType.ShoeFootprint;

        if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var suit)
            && TryComp<PressureProtectionComponent>(suit, out _))
            state = FootprintVisualType.SuitFootprint;

        return state;
    }
    #endregion
}
