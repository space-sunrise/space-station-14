namespace Content.Server.Mobs.Components;
/// <summary>
/// Хранит последние слова до подтверждения смерти.
/// </summary>
[RegisterComponent]
public sealed partial class PendingLastWordsComponent : Component
{
    [DataField]
    public string Text = string.Empty;
}