using Content.Shared.Damage.Components;

namespace Content.Shared.Damage.Systems;

// Возможно оффы позже добавят
public abstract partial class SharedStaminaSystem
{
    /// <summary>
    /// Восстанавливает выносливость.
    /// </summary>
    public void RestoreStamina(EntityUid uid, float amount)
    {
        if (amount <= 0f || !TryComp<StaminaComponent>(uid, out var stamina))
            return;

        if (stamina.Critical)
            ExitStamCrit(uid, stamina);

        TakeStaminaDamage(uid, -amount, stamina, visual: false, ignoreResist: true);
    }
}
