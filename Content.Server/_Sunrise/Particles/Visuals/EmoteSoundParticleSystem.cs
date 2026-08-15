using Content.Shared._Sunrise.Particles;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Particles;

/// <summary>
/// Resolves confirmed audible emotes to data-driven particle visuals.
/// </summary>
public sealed class EmoteSoundParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly Dictionary<ProtoId<EmotePrototype>, ProtoId<ParticleOrchestraPrototype>> _visuals = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteSoundPlayedParticleEvent>(OnEmoteSoundPlayed);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        RebuildVisuals();
    }

    private void OnEmoteSoundPlayed(ref EmoteSoundPlayedParticleEvent args)
    {
        if (TerminatingOrDeleted(args.Source) ||
            !_visuals.TryGetValue(new ProtoId<EmotePrototype>(args.EmoteId), out var orchestra))
        {
            return;
        }

        _orchestra.Send(orchestra, args.Source);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EmoteParticleVisualsPrototype>())
            RebuildVisuals();
    }

    private void RebuildVisuals()
    {
        _visuals.Clear();
        foreach (var group in _proto.EnumeratePrototypes<EmoteParticleVisualsPrototype>())
        {
            foreach (var emote in group.Emotes)
            {
                if (_visuals.TryAdd(emote, group.Orchestra))
                    continue;

                Log.Error($"Emote particle visual '{emote}' is assigned by more than one prototype");
            }
        }
    }
}
