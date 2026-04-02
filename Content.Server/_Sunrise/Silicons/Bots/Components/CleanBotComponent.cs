using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.Silicons.Bots.Components;

[RegisterComponent]
public sealed partial class CleanBotComponent : Component
{
    [DataField]
    public float WantedVisionRange = 8f;

    [DataField]
    public float ChaseLoseRange = 12f;

    [DataField]
    public float RetaliationTime = 20f;

    [DataField]
    public float UpdateInterval = 0.25f;

    [DataField]
    public float RetaliationStaminaDamage = 35f;

    [DataField]
    public SoundSpecifier? RetaliationStaminaSound = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");

    [DataField]
    public ProtoId<SecurityIconPrototype> WantedStatusIcon = "SecurityIconWanted";

    [ViewVariables]
    public EntityUid? WantedTarget;

    [ViewVariables]
    public EntityUid? RetaliationTarget;

    [ViewVariables]
    public TimeSpan RetaliationEndTime;

    [ViewVariables]
    public TimeSpan NextUpdateTime;

    [ViewVariables]
    public bool BatonMode;
}
