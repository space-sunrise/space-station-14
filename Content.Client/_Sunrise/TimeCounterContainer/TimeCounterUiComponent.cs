using Content.Client._Sunrise.TimeCounterContainer;

namespace Content.Client._Sunrise.TimeCounterContainer;

[RegisterComponent]
public sealed partial class TimeCounterUiComponent : Component
{
    public TimeCounter? Counter;
}
