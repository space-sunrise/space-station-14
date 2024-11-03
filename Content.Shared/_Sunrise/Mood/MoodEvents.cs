using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Mood;

[Serializable, NetSerializable]
public sealed class MoodEffectEvent(string effectId, float effectModifier = 1f, float effectOffset = 0f) : EntityEventArgs
{
    public string EffectId = effectId;

    public float EffectModifier = effectModifier;

    public float EffectOffset = effectOffset;
}

[Serializable, NetSerializable]
public sealed class MoodRemoveEffectEvent(string effectId) : EntityEventArgs
{
    public string EffectId = effectId;
}

[ByRefEvent]
public record struct OnSetMoodEvent(EntityUid Receiver, float MoodChangedAmount, bool Cancelled);

[ByRefEvent]
public record struct OnMoodEffect(EntityUid Receiver, string EffectId, float EffectModifier = 1, float EffectOffset = 0);
