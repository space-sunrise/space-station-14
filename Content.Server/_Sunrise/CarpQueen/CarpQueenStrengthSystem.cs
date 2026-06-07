using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.CombatMode;
using Content.Shared.Movement.Components;

namespace Content.Server._Sunrise.CarpQueen;

/// <summary>
/// Система увеличивает силу толкания королевы карпов в боевом режиме.
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

        // Базовая сила для обычного режима и усиленная сила для боевого режима.
        const float baseStrength = 50f;
        const float combatStrength = 200f; // В боевом режиме в 4 раза сильнее.

        mobCollision.Strength = args.IsInCombatMode ? combatStrength : baseStrength;
        Dirty(uid, mobCollision);
    }
}
