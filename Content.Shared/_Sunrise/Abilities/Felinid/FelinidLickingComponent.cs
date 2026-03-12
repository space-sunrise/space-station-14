using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Abilities.Felinid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FelinidLickingComponent : Component
{
    [DataField]
    public EntProtoId ActionLickingWoundsId = "ActionLickingWounds";

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public bool StopBleeding = true;

    [DataField]
    public float BloodlossModifier = -1.0f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3f);

    [DataField]
    public SoundSpecifier? HealingBeginSound;

    [DataField]
    public SoundSpecifier? HealingEndSound;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Action;
}
