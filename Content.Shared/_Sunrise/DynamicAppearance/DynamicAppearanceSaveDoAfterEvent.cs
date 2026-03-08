using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.DynamicAppearance;

[Serializable, NetSerializable]
public sealed partial class DynamicAppearanceSaveDoAfterEvent : DoAfterEvent
{
    public DynamicAppearanceState State;

    public DynamicAppearanceSaveDoAfterEvent(DynamicAppearanceState state)
    {
        State = state;
    }

    public override DoAfterEvent Clone() => this;
}