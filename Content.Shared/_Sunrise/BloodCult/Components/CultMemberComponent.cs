using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.BloodCult.Components;

/// <summary>
/// This is used for tagging a mob as a cultist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultMemberComponent : Component
{
    [DataField]
    public EntityUid? LastAttackedEntity = null;

    [DataField]
    public TimeSpan? NextPopupTime = null;

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(3.0);

    [DataField]
    public string Reason = "Вы не можете атаковать членов культа";
}
