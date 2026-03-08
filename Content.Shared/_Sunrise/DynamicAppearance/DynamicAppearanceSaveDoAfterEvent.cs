using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
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

    public override DoAfterEvent Clone()
    {
        return new DynamicAppearanceSaveDoAfterEvent(new DynamicAppearanceState(
            new MarkingSet(State.MarkingSet),
            State.Species,
            State.Sex,
            State.Age,
            State.Gender,
            State.Voice,
            State.SkinColor,
            State.EyeColor,
            new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(State.CustomBaseLayers),
            State.BodyType,
            State.Width,
            State.Height,
            State.Name));
    }
}
