using Content.Server.Atmos.Components;
using Content.Server.Gravity;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Footprints;

/// <summary>
/// Handles creation and management of footprints left by entities as they move.
/// </summary>
public sealed class FootprintSystem : EntitySystem
{
    #region Dependencies

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    #endregion

    #region Entity Queries

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<SolutionContainerManagerComponent> _solutionQuery;
    private EntityQuery<StandingStateComponent> _standingQuery;
    private EntityQuery<FootprintComponent> _footprintQuery;
    private EntityQuery<PressureProtectionComponent> _pressureQuery;

    #endregion

    public static readonly float FootsVolume = 5;
    public static readonly float BodySurfaceVolume = 15;

    // Dictionary to track footprints per tile to prevent overcrowding
    private const int MaxFootprintsPerTile = 6;
    private const int MaxMarksPerTile = 3;

    private readonly HashSet<Entity<FootprintComponent>> _entities = [];

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
        _solutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();
        _standingQuery = GetEntityQuery<StandingStateComponent>();
        _footprintQuery = GetEntityQuery<FootprintComponent>();
        _pressureQuery = GetEntityQuery<PressureProtectionComponent>();

        SubscribeLocalEvent<FootprintEmitterComponent, ComponentStartup>(OnEmitterStartup);
        SubscribeLocalEvent<FootprintEmitterComponent, MoveEvent>(OnEntityMove);
        SubscribeLocalEvent<FootprintEmitterComponent, ComponentInit>(OnFootprintEmitterInit);
    }

    private void OnFootprintEmitterInit(Entity<FootprintEmitterComponent> entity, ref ComponentInit args)
    {
        _solution.EnsureSolution(entity.Owner, entity.Comp.FootsSolutionName, out _, FixedPoint2.New(FootsVolume));
        _solution.EnsureSolution(entity.Owner, entity.Comp.BodySurfaceSolutionName, out _, FixedPoint2.New(BodySurfaceVolume));
    }

    /// <summary>
    /// Handles initialization of footprint emitter components.
    /// </summary>
    private void OnEmitterStartup(Entity<FootprintEmitterComponent> ent, ref ComponentStartup args)
    {
        // Add small random variation to step interval
        ent.Comp.WalkStepInterval = Math.Max(0f, ent.Comp.WalkStepInterval + _random.NextFloat(-0.05f, 0.05f));
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles entity movement and creates footprints when appropriate.
    /// </summary>
    private void OnEntityMove(Entity<FootprintEmitterComponent> ent, ref MoveEvent args)
    {
        if (!_solutionQuery.TryComp(ent, out var container))
            return;

        // Eсли нет компонента StandingState, считаем, что мы стоим. Следы приоритетнее, чем мазня.
        var stand = !_standingQuery.TryComp(ent, out var standing) || standing.Standing;

        var solCont = (ent, container);
        Solution solution;
        Entity<SolutionComponent> solComp;

        if (stand)
        {
            if (!_solution.ResolveSolution(solCont, ent.Comp.FootsSolutionName, ref ent.Comp.FootsSolution, out var footsSolution))
                return;

            solution = footsSolution;
            solComp = ent.Comp.FootsSolution.Value;
        }
        else
        {
            if (!_solution.ResolveSolution(solCont, ent.Comp.BodySurfaceSolutionName, ref ent.Comp.BodySurfaceSolution, out var bodySurfaceSolution))
                return;

            solution = bodySurfaceSolution;
            solComp = ent.Comp.BodySurfaceSolution.Value;
        }

        if (solution.Volume <= 0)
            return;

        // Check if footprints should be created
        if (!_physicsQuery.TryComp(ent, out var body))
            return;

        if (body.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(ent.Owner))
            return;

        var transform = Transform(ent);
        var mapCoords = _transform.GetMapCoordinates((ent, transform));
        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
            return;

        var distanceMoved = (transform.LocalPosition - ent.Comp.LastStepPosition).Length();
        var requiredDistance = stand ? ent.Comp.WalkStepInterval : ent.Comp.DragMarkInterval;

        if (!(distanceMoved > requiredDistance))
            return;

        var tileRef = _mapSystem.GetTileRef((gridUid, grid), transform.Coordinates);

        _entities.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tileRef.GridIndices, _entities);
        var dragMarkCount = 0;
        var footPrintCount = 0;

        foreach (var footPrint in _entities)
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

        ent.Comp.IsRightStep = !ent.Comp.IsRightStep;

        // Create new footprint entity
        var footprintEntity = SpawnFootprint(gridUid, ent.Comp, solution, ent, transform, stand);

        // Update footprint and emitter state
        UpdateFootprint(footprintEntity, (ent, ent.Comp), solComp, transform, stand);

        // Update emitter state.
        UpdateEmitterState(ent.Comp, transform);
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

        var appearance = UpdateAppearance(entity, emitterSolution);

        if (appearance == null)
            return entity;

        var visualType = DetermineVisualState(emitterOwner, stand);
        _appearance.SetData(entity,
            FootprintVisualParameter.VisualState,
            GetStateId(visualType, emitter),
            appearance);

        return entity;
    }

    private AppearanceComponent? UpdateAppearance(EntityUid entity, Solution emitterSolution)
    {
        if (!_appearanceQuery.TryComp(entity, out var appearance))
            return null;

        var t = emitterSolution.Volume.Float() / emitterSolution.MaxVolume.Float();
        t = Easings.OutQuad(t);
        t = MathF.Pow(t, 0.7f);
        var alpha = Math.Clamp(0.05f + t * 0.7f, 0f, 1f);

        _appearance.SetData(entity,
            FootprintVisualParameter.TrackColor,
            emitterSolution.GetColor(_prototype).WithAlpha(alpha),
            appearance);

        return appearance;
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

            _ => throw new InvalidOperationException($"Unknown footprint visual type: {visualType}")
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
        if (!_solutionQuery.TryComp(footprintEntity, out var container)
            || !_footprintQuery.TryComp(footprintEntity, out var footprint)
            || !_solution.ResolveSolution((footprintEntity, container),
                footprint.ContainerName,
                ref footprint.SolutionContainer,
                out _))
            return;

        var splitSolution = _solution.SplitSolution(emitterSolution, stand ? emitter.Comp.TransferVolumeFoot : emitter.Comp.TransferVolumeDragMark);

        _solution.AddSolution(footprint.SolutionContainer.Value, splitSolution);
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
            && _pressureQuery.TryComp(suit, out _))
            state = FootprintVisualType.SuitFootprint;

        return state;
    }
    #endregion
}
