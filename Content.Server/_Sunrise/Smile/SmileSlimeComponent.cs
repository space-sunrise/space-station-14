namespace Content.Server._Sunrise.Smile;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SmileSlimeComponent : Component
{
    [DataField]
    public TimeSpan? PrevPopup;

    [DataField]
    public TimeSpan PopupDelay = TimeSpan.FromSeconds(1);

    // [DataField]
    // public string PopupText = "goodfeeling-artifact-";

    [DataField("messages")]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> Messages = default!;
}
