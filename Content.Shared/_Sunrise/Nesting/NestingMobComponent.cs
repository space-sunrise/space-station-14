using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Nesting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NestingMobComponent : Component
{
    [DataField]
    public float DefaultDoAfterLength = 3.0f;

    [DataField]
    public float DeadDoAfterLength = 1.0f;

    [DataField, AutoNetworkedField]
    public bool InContainer;
}
