using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Shared system for ninja equipment that draws power from the ninja suit's battery.
/// The actual power draw logic is handled serverside.
/// </summary>
public abstract class SharedNinjaSuitDrawSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitDrawComponent, ComponentShutdown>(OnComponentShutdown);
    }

    /// <summary>
    /// Disable power draw when component is removed.
    /// </summary>
    private void OnComponentShutdown(Entity<NinjaSuitDrawComponent> ent, ref ComponentShutdown args)
    {
        SetEnabled(ent, false);
    }

    /// <summary>
    /// Enable or disable power drawing for this entity.
    /// </summary>
    public void SetEnabled(Entity<NinjaSuitDrawComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Check if the entity can draw power from the ninja suit.
    /// </summary>
    public virtual bool CanDrawPower(Entity<NinjaSuitDrawComponent> ent)
    {
        return ent.Comp.CanDraw;
    }

    /// <summary>
    /// Check if the entity has sufficient power to use.
    /// </summary>
    public virtual bool CanUse(Entity<NinjaSuitDrawComponent> ent)
    {
        return ent.Comp.CanUse;
    }
}