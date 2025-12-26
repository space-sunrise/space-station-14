using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities.Felinid;

[RegisterComponent]
public sealed partial class FelinidLickingComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionLickingWoundsId = "ActionLickingWounds";

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public bool StopBleeding = true;

    [DataField]
    public float BloodlossModifier = -1.0f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3f);

    [DataField]
    public SoundSpecifier? HealingBeginSound = null;

    [DataField]
    public SoundSpecifier? HealingEndSound = null;
}
