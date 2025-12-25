using Content.Server.Body.Systems;
using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CarpQueen;

/// <summary>
/// System that handles carp servant bite mechanics:
/// Injects 1u of each remembered reagent from the liquid the carp hatched from.
/// </summary>
public sealed class CarpServantBiteSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarpServantMemoryComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, CarpServantMemoryComponent memory, MeleeHitEvent args)
    {
        if (memory.RememberedReagents.Count == 0)
            return;

        // Inject 1u of each remembered reagent into each hit target
        foreach (var target in args.HitEntities)
        {
            if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
                continue;

            // Create solution with configured amount of each remembered reagent
            var solution = new Solution();
            foreach (var (reagentId, _) in memory.RememberedReagents)
            {
                if (_protos.HasIndex<ReagentPrototype>(reagentId))
                {
                    solution.AddReagent(reagentId, memory.BiteReagentAmount);
                }
            }

            if (solution.Volume > FixedPoint2.Zero)
            {
                _bloodstream.TryAddToChemicals((target, bloodstream), solution);
            }
        }
    }
}

