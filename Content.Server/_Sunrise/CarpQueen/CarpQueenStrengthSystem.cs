using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.CombatMode;
using Content.Shared.Movement.Components;

namespace Content.Server._Sunrise.CarpQueen;

/// <summary>
/// System that increases Carp Queen's pushing strength when in combat mode.
/// </summary>
public sealed class CarpQueenStrengthSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarpQueenComponent, CombatModeChangedEvent>(OnCombatModeChanged);
    }

    private void OnCombatModeChanged(EntityUid uid, CarpQueenComponent component, ref CombatModeChangedEvent args)
    {
        if (!TryComp<MobCollisionComponent>(uid, out var mobCollision))
            return;

        // Base strength for normal mode, boosted strength for combat mode
        const float baseStrength = 50f;
        const float combatStrength = 200f; // 4x stronger in combat mode

        mobCollision.Strength = args.IsInCombatMode ? combatStrength : baseStrength;
        Dirty(uid, mobCollision);
    }
}

