using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Shower.Components;
using Content.Shared.Shower.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Shower;

/// <summary>
/// Server-side shower system that handles water effects, reagent cleaning, and puddle creation.
/// </summary>
public sealed class ShowerSystem : SharedShowerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowerComponent, ComponentInit>(OnShowerInit);
        SubscribeLocalEvent<ShowerComponent, ComponentShutdown>(OnShowerShutdown);
    }

    private void OnShowerInit(Entity<ShowerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.CleaningAccumulator = 0f;
        ent.Comp.SteamAccumulator = 0f;
    }

    private void OnShowerShutdown(Entity<ShowerComponent> ent, ref ComponentShutdown args)
    {
        // Stop playing water sound when component is removed
        if (ent.Comp.PlayingSound != null)
        {
            _audio.Stop(ent.Comp.PlayingSound.Value);
            ent.Comp.PlayingSound = null;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShowerComponent>();
        while (query.MoveNext(out var uid, out var shower))
        {
            if (!shower.IsOn)
                continue;

            // Update cleaning accumulator
            shower.CleaningAccumulator += frameTime;

            if (shower.CleaningAccumulator >= shower.CleaningInterval)
            {
                shower.CleaningAccumulator = 0f;
                ProcessShowerEffects(uid, shower);
            }

            // Handle steam spawning - continuous cycle every 3 seconds
            shower.SteamAccumulator += frameTime;

            if (shower.SteamAccumulator >= shower.SteamInterval)
            {
                shower.SteamAccumulator = 0f;
                SpawnSteamEffect(uid);
            }
        }
    }

    public override void ToggleShower(Entity<ShowerComponent> ent, EntityUid? user = null)
    {
        base.ToggleShower(ent, user);

        if (ent.Comp.IsOn)
        {
            // Reset steam timer when turning on
            ent.Comp.SteamAccumulator = 0f;

            // Start playing water sound continuously (looped)
            var audioParams = AudioParams.Default.WithLoop(true).WithVolume(-5f);
            var soundEntity = _audio.PlayPvs(ent.Comp.WaterSound, ent.Owner, audioParams);
            ent.Comp.PlayingSound = soundEntity?.Entity;

            // Spawn first steam immediately
            SpawnSteamEffect(ent.Owner);

            // Create or maintain water puddle when turning on
            CreateOrMaintainWaterPuddle(ent);

            // Clean puddles in 3x3 area around shower
            CleanPuddlesAroundShower(ent.Owner);
        }
        else
        {
            // Stop playing water sound when turning off
            if (ent.Comp.PlayingSound != null)
            {
                _audio.Stop(ent.Comp.PlayingSound.Value);
                ent.Comp.PlayingSound = null;
            }
        }
    }

    private void ProcessShowerEffects(EntityUid showerUid, ShowerComponent shower)
    {
        var showerCoords = _transform.GetMoverCoordinates(showerUid);

        // Find all entities on the same tile as the shower
        var entitiesOnTile = new HashSet<EntityUid>();
        var lookupSystem = EntityManager.System<EntityLookupSystem>();

        foreach (var entity in lookupSystem.GetEntitiesIntersecting(showerCoords.ToMap(EntityManager, _transform)))
        {
            entitiesOnTile.Add(entity);
        }

        // Clean reagents from entities with body components (players, mobs)
        foreach (var entity in entitiesOnTile)
        {
            if (HasComp<BodyComponent>(entity))
            {
                CleanEntityReagents(entity);
            }
        }

        // Maintain water puddle
        CreateOrMaintainWaterPuddle((showerUid, shower));
    }

    private void CleanEntityReagents(EntityUid entity)
    {
        // Try to clean external reagents (on skin/clothes)
        if (TryComp<SolutionContainerManagerComponent>(entity, out var solutionManager))
        {
            // Look for external reagent containers (like bloodstream external)
            if (_solutionSystem.TryGetSolution(entity, "external", out var externalSolution, out var externalSolutionComp))
            {
                // Remove a portion of external reagents (simulating washing off)
                _solutionSystem.RemoveAllSolution(externalSolution.Value);
            }

            // Clean footprint reagents from feet (removes dirty footprints)
            if (_solutionSystem.TryGetSolution(entity, "foots", out var footsSolution, out var footsSolutionComp))
            {
                _solutionSystem.RemoveAllSolution(footsSolution.Value);
            }

            // Clean body surface reagents (for when lying down)
            if (_solutionSystem.TryGetSolution(entity, "body_surface", out var bodySolution, out var bodySolutionComp))
            {
                _solutionSystem.RemoveAllSolution(bodySolution.Value);
            }

            // Also clean "puddle" solution if entity stepped in something
            if (_solutionSystem.TryGetSolution(entity, "puddle", out var puddleSolution, out var puddleSolutionComp))
            {
                _solutionSystem.RemoveAllSolution(puddleSolution.Value);
            }
        }
    }

    private void CreateOrMaintainWaterPuddle(Entity<ShowerComponent> ent)
    {
        var coords = _transform.GetMoverCoordinates(ent.Owner);

        // Check if there's already a puddle at this location
        var existingPuddles = new List<EntityUid>();
        var lookupSystem = EntityManager.System<EntityLookupSystem>();

        foreach (var entity in lookupSystem.GetEntitiesIntersecting(coords.ToMap(EntityManager, _transform)))
        {
            if (HasComp<PuddleComponent>(entity))
            {
                existingPuddles.Add(entity);
            }
        }

        if (existingPuddles.Count > 0)
        {
            // Maintain existing puddle - ensure it has the right amount of water
            var puddle = existingPuddles[0];
            if (_solutionSystem.TryGetSolution(puddle, "puddle", out var solutionEnt, out var solutionComp))
            {
                // If puddle has less than desired amount, add water
                if (solutionComp.Volume < ent.Comp.PuddleWaterAmount)
                {
                    var waterToAdd = ent.Comp.PuddleWaterAmount - solutionComp.Volume;
                    var waterSolution = new Solution();
                    waterSolution.AddReagent("Water", waterToAdd);
                    _solutionSystem.TryAddSolution(solutionEnt.Value, waterSolution);
                }
            }
        }
        else
        {
            // Create new water puddle
            var waterSolution = new Solution();
            waterSolution.AddReagent("Water", ent.Comp.PuddleWaterAmount);
            _puddle.TrySpillAt(coords, waterSolution, out _, false);
        }
    }

    private void CleanPuddlesAroundShower(EntityUid showerUid)
    {
        var showerCoords = _transform.GetMoverCoordinates(showerUid);
        var lookupSystem = EntityManager.System<EntityLookupSystem>();

        // Check 3x3 area around shower (radius of 1.5 tiles)
        var searchArea = new Box2(showerCoords.Position - new System.Numerics.Vector2(1.5f, 1.5f),
                                  showerCoords.Position + new System.Numerics.Vector2(1.5f, 1.5f));

        var puddlesToClean = new List<EntityUid>();

        // Find all puddles in the area
        foreach (var entity in lookupSystem.GetEntitiesIntersecting(showerCoords.ToMap(EntityManager, _transform)))
        {
            if (HasComp<PuddleComponent>(entity))
            {
                var entityCoords = _transform.GetMoverCoordinates(entity);
                var distance = (entityCoords.Position - showerCoords.Position).Length();

                // Check if within 3 tile radius (approximately 1.5 units)
                if (distance <= 1.5f)
                {
                    puddlesToClean.Add(entity);
                }
            }
        }

        // Replace puddle contents with water
        foreach (var puddle in puddlesToClean)
        {
            if (_solutionSystem.TryGetSolution(puddle, "puddle", out var solutionEnt, out var solutionComp))
            {
                // Get current volume
                var currentVolume = solutionComp.Volume;

                // Clear all reagents and replace with water
                _solutionSystem.RemoveAllSolution(solutionEnt.Value);

                // Add water solution
                var waterSolution = new Solution();
                waterSolution.AddReagent("Water", currentVolume);
                _solutionSystem.TryAddSolution(solutionEnt.Value, waterSolution);
            }
        }
    }

    private void SpawnSteamEffect(EntityUid showerUid)
    {
        var coords = _transform.GetMoverCoordinates(showerUid);

        // Spawn steam slightly above the shower
        var steamCoords = coords.Offset(new System.Numerics.Vector2(_random.NextFloat(-0.3f, 0.3f), _random.NextFloat(0.2f, 0.5f)));

        if (_prototypeManager.HasIndex<EntityPrototype>("ShowerSteam"))
        {
            Spawn("ShowerSteam", steamCoords);
        }
    }
}
