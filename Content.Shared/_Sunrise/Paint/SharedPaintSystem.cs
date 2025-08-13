namespace Content.Shared._Sunrise.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public abstract class SharedPaintSystem : EntitySystem
{
    public virtual void UpdateAppearance(EntityUid uid, SprayPaintedComponent? component = null)
    {
    }
}
