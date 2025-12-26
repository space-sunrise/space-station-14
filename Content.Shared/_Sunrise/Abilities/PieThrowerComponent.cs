using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class PieThrowerComponent : Component
{
    [DataField]
    public EntProtoId PieProtoId = "FoodPieBananaCream";

    [DataField]
    public EntProtoId ActionPieThrow = "PieThrow";

    [DataField]
    public SoundSpecifier? SoundThrowPie = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");
}


public sealed partial class PieThrowActionEvent : WorldTargetActionEvent
{

}
