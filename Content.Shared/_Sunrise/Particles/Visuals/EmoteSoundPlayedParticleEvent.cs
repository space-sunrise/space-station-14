namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Reports that an emote sound actually started playing.
/// </summary>
[ByRefEvent]
public readonly record struct EmoteSoundPlayedParticleEvent(EntityUid Source, string EmoteId);
