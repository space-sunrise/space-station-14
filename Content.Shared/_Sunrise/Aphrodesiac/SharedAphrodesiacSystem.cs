using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;

namespace Content.Shared._Sunrise.Aphrodesiac;

public abstract class SharedAphrodesiacSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string LoveKey = "LoveEffect";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public void TryApplyLoveenness(EntityUid uid, float effectPower, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, LoveKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<LoveVisionComponent>(uid, LoveKey, TimeSpan.FromSeconds(effectPower), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, LoveKey, TimeSpan.FromSeconds(effectPower), status);
        }
    }

    public void TryRemoveLovenness(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, LoveKey);
    }
    public void TryRemoveLovenessTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, LoveKey, TimeSpan.FromSeconds(timeRemoved));
    }

}
