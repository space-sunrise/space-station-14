using Robust.Shared.Audio.Systems;

namespace Content.Shared._Sunrise.TapePlayer;

public abstract class SharedTapePlayerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
}
