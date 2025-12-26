using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.VentCraw;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VentCrawlerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool InTube = false;

    public float EnterDelay = 2.5f;
}


[Serializable, NetSerializable]
public sealed partial class EnterVentDoAfterEvent : SimpleDoAfterEvent
{
}
