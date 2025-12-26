namespace Content.Client.Holiday;

[RegisterComponent]
public sealed partial class HolidayClothingSwapComponent : Component
{
    [DataField]
    public Dictionary<string, string> Sprite = new();
}
