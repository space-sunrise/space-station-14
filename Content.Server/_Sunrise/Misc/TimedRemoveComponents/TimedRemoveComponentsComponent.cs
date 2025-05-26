using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Misc.TimedRemoveComponents;

/// <summary>
/// Компонент, который автоматически убирает переданные компоненты через переданное время
/// </summary>
[RegisterComponent]
public sealed partial class TimedRemoveComponentsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField]
    public TimeSpan RemoveAfter = TimeSpan.FromSeconds(5);
}
