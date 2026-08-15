using Content.Shared._Sunrise.Events;
using Content.Shared.Trigger;

namespace Content.Shared._Sunrise.Trigger.TriggerOnReflected;

/// <summary>
/// Bridges reflected attacks into the generic trigger pipeline.
/// </summary>
public sealed class TriggerOnReflectedSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnReflectedComponent, ReflectedEvent>(OnReflected);
    }

    private void OnReflected(Entity<TriggerOnReflectedComponent> ent, ref ReflectedEvent args)
    {
        Trigger.Trigger(ent, args.Shooter, ent.Comp.KeyOut, predicted: false);
    }
}
