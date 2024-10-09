namespace Content.Server._Sunrise.Antag.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class MultiAntagSelectionComponent : Component
{
    [DataField]
    public List<Component> Components;
}
