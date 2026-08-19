using Content.Shared.NPC.Components;
using Content.Shared.Stealth.Components;

namespace Content.Shared.Stealth;

public abstract partial class SharedStealthSystem
{
    private void OnShutdown(EntityUid uid, StealthComponent component, ComponentShutdown args)
    {
        UpdateNoTarget(uid, false);
    }
    private void UpdateNoTarget(EntityUid uid, bool stealthEnabled)
    {
        if (stealthEnabled)
            EnsureComp<NoTargetComponent>(uid);
        else if (HasComp<NoTargetComponent>(uid))
            RemComp<NoTargetComponent>(uid);
    }
}
