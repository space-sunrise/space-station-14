using Content.Server.Atmos.Components;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.GameTicking;

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
    #endregion

    #region Entity Queries
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobThresholdsComponent> _mobStateQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    #endregion

    // Dictionary to track footprints per tile to prevent overcrowding
    private readonly Dictionary<(EntityUid GridId, Vector2i TilePosition), HashSet<EntityUid>> _tileFootprints = new();
    private const int MaxFootprintsPerTile = 2; // Maximum footprints allowed per tile

    #region Initialization
    /// <summary>
    /// Initializes the footprint system and sets up required queries and subscriptions.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _mobStateQuery = GetEntityQuery<MobThresholdsComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<FootprintEmitterComponent, ComponentStartup>(OnEmitterStartup);
        SubscribeLocalEvent<FootprintEmitterComponent, MoveEvent>(OnEntityMove);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
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
        if (emitter.TrackColor.A <= 0f
            || !_transformQuery.TryComp(uid, out var transform)
            || !_mobStateQuery.TryComp(uid, out var mobState)
            || !_mapManager.TryFindGridAt(_transformSystem.GetMapCoordinates((uid, transform)), out var gridUid, out var grid))
            return;

        var isBeingDragged =
            mobState.CurrentThresholdState is MobState.Critical or MobState.Dead ||
            (TryComp<StandingStateComponent>(uid, out var stateComponent) &&
             stateComponent.CurrentState is StandingState.Lying);

        var distanceMoved = (transform.LocalPosition - emitter.LastStepPosition).Length();
        var requiredDistance = isBeingDragged ? emitter.DragMarkInterval : emitter.WalkStepInterval;

        if (!(distanceMoved > requiredDistance))
            return;

        var tilePos = grid.TileIndicesFor(transform.Coordinates);
        var tileKey = (gridUid, tilePos);

        if (_tileFootprints.TryGetValue(tileKey, out var existingPrints) &&
            existingPrints.Count >= MaxFootprintsPerTile)
        {
            if (existingPrints.Count > 0)
            {
                var oldestPrint = existingPrints.First();
                existingPrints.Remove(oldestPrint);
                QueueDel(oldestPrint);
            }
        }

        emitter.IsRightStep = !emitter.IsRightStep;

        // Create new footprint entity
        var footprintEntity = SpawnFootprint(gridUid, emitter, uid, transform, isBeingDragged);

        // Add the new footprint to tile tracking
        if (!_tileFootprints.ContainsKey(tileKey))
            _tileFootprints[tileKey] = new HashSet<EntityUid>();

        _tileFootprints[tileKey].Add(footprintEntity);

        // Update footprint and emitter state
        UpdateFootprint(footprintEntity, emitter, transform, isBeingDragged);
        UpdateEmitterState(emitter, transform);
    }

    private void Reset(RoundRestartCleanupEvent msg)
    {
        _tileFootprints.Clear();
    }

    #endregion

    #region Footprint Creation and Management
    /// <summary>
    /// Creates a new footprint entity at the calculated position.
    /// </summary>
    private EntityUid SpawnFootprint(
        EntityUid gridUid,
        FootprintEmitterComponent emitter,
        EntityUid emitterOwner,
        TransformComponent transform,
        bool isDragging)
    {
        var coords = CalculateFootprintPosition(gridUid, emitter, transform, isDragging);
        var entity = Spawn(emitter.FootprintPrototype, coords);

        var footprint = EnsureComp<FootprintComponent>(entity);
        footprint.CreatorEntity = emitterOwner;
        Dirty(entity, footprint);

        if (_appearanceQuery.TryComp(entity, out var appearance))
        {
            _appearanceSystem.SetData(entity,
                FootprintVisualParameter.VisualState,
                DetermineVisualState(emitterOwner, isDragging),
                appearance);

            _appearanceSystem.SetData(entity,
                FootprintVisualParameter.TrackColor,
                emitter.TrackColor,
                appearance);
        }

        return entity;
    }

    /// <summary>
    /// Updates footprint rotation and reagent transfer.
    /// </summary>
    private void UpdateFootprint(
        EntityUid footprintEntity,
        FootprintEmitterComponent emitter,
        TransformComponent transform,
        bool isDragging)
    {
        if (!_transformQuery.TryComp(footprintEntity, out var footprintTransform))
            return;

        footprintTransform.LocalRotation = isDragging
            ? (transform.LocalPosition - emitter.LastStepPosition).ToAngle() + Angle.FromDegrees(-90f)
            : transform.LocalRotation + Angle.FromDegrees(180f);

        TransferReagents(footprintEntity, emitter);
    }
    #endregion

    #region State Management
    /// <summary>
    /// Updates emitter state after creating a footprint.
    /// </summary>
    private void UpdateEmitterState(FootprintEmitterComponent emitter, TransformComponent transform)
    {
        emitter.TrackColor = emitter.TrackColor.WithAlpha(Math.Max(0f, emitter.TrackColor.A - emitter.ColorFadeRate));
        emitter.LastStepPosition = transform.LocalPosition;
    }

    /// <summary>
    /// Transfers reagents from emitter to footprint if applicable.
    /// </summary>
    private void TransferReagents(EntityUid footprintEntity, FootprintEmitterComponent emitter)
    {
        if (!TryComp<SolutionContainerManagerComponent>(footprintEntity, out var container)
            || !TryComp<FootprintComponent>(footprintEntity, out var footprint)
            || !_solutionSystem.ResolveSolution((footprintEntity, container),
                footprint.ContainerName,
                ref footprint.SolutionContainer,
                out var solution)
            || string.IsNullOrWhiteSpace(emitter.CurrentReagent)
            || solution.Volume >= 1)
            return;

        _solutionSystem.TryAddReagent(footprint.SolutionContainer.Value,
            emitter.CurrentReagent,
            1,
            out _);
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
        bool isDragging)
    {
        if (isDragging)
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
    private FootprintVisualType DetermineVisualState(EntityUid uid, bool isDragging)
    {
        if (isDragging)
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
