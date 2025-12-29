using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Medical.PsychologistSystem;

public sealed partial class AlcoholBlockEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DoAfterAlcoholBlockEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
