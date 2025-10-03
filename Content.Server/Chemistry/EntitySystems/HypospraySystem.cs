using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Hypospray.Events;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// Server-side system for hypospray that adds borg announcement functionality
/// </summary>
public sealed class ServerHypospraySystem : EntitySystem
{
    [Dependency] private readonly Content.Server._Sunrise.Medical.BorgHypospraySystem _borgHypospray = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to injection events after the shared system processes them
        SubscribeLocalEvent<HyposprayComponent, HyposprayAfterInjectEvent>(OnAfterInject);
    }

    private void OnAfterInject(Entity<HyposprayComponent> entity, ref HyposprayAfterInjectEvent args)
    {
        // Get the solution that was injected
        if (_solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var hypoSpraySoln, out _))
        {
            _borgHypospray.TryAnnounceInjection(entity.Owner, args.User, args.Target, hypoSpraySoln.Value);
        }
    }
}