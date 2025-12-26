using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentTarget;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, TimeSpan> InteractionCooldowns = new();
}
