using Content.Shared.Alert;

namespace Content.Shared._Sunrise.Mood;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class NetMoodComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CurrentMoodLevel;

    [DataField, AutoNetworkedField]
    public float NeutralMoodThreshold;
}

public sealed partial class ShowMoodEffectsAlertEvent : BaseAlertEvent;
