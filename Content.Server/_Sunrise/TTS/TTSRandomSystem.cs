using Content.Shared._Sunrise.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.TTS;

/// <summary>
/// Система, выбирающая случайный TTS голос из списка с шансами при спавне entity
/// </summary>
public sealed class TTSRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TTSRandomComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<TTSRandomComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<TTSComponent>(ent, out var ttsComponent))
            return;

        if (ttsComponent.VoicePrototypeId.HasValue)
            return;

        var selectedVoice = SelectRandomVoice(ent.Comp.Voices);
        if (selectedVoice.HasValue)
        {
            ttsComponent.VoicePrototypeId = selectedVoice.Value;
            Dirty(ent, ttsComponent);
        }
    }

    private ProtoId<TTSVoicePrototype>? SelectRandomVoice(Dictionary<ProtoId<TTSVoicePrototype>, int> voices)
    {
        if (voices.Count == 0)
            return null;

        var totalChance = 0;
        foreach (var chance in voices.Values)
        {
            totalChance += Math.Max(1, chance);
        }

        if (totalChance == 0)
            return null;

        var randomValue = _random.Next(totalChance);
        var currentChance = 0;

        foreach (var (voiceId, chance) in voices)
        {
            currentChance += Math.Max(1, chance);
            if (randomValue < currentChance)
            {
                return voiceId;
            }
        }

        return voices.Keys.First();
    }
}
