namespace Content.Shared._Sunrise.Movement.Pulling;

/// <summary>
/// Добавляется, пока у сущности активен визуальный эффект притягивания.
/// </summary>
[RegisterComponent, Access(typeof(SharedPullingAnimationSystem))]
public sealed partial class ActivePullingAnimationComponent : Component
{
    /// <summary>
    /// Визуальный эффект, прикрепленный к притягиваемой сущности.
    /// </summary>
    [ViewVariables]
    public EntityUid? Effect;
}
