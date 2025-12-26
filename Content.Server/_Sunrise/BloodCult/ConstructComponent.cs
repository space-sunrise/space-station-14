namespace Content.Server._Sunrise.BloodCult;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    [DataField("actions")]
    public List<string> Actions = new();
}
