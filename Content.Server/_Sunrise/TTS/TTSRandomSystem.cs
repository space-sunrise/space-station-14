using System.Linq;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

    private ProtoId<TTSVoicePrototype>? SelectRandomVoice(Dictionary<string, int> voices)
    {
        if (voices.Count == 0)
            return null;

        var totalChance = CalculateTotalChance(voices);
        if (totalChance <= 0)
            return null;

        var randomRoll = _random.Next(totalChance);
        var currentThreshold = 0;

        foreach (var (voiceKey, chance) in voices)
        {
            currentThreshold += Math.Max(1, chance);
            if (randomRoll < currentThreshold)
            {
                return ResolveVoicePrototype(voiceKey);
            }
        }

        return ResolveVoicePrototype(voices.Keys.First());
    }

    private int CalculateTotalChance(Dictionary<string, int> voices)
    {
        var total = 0;
        foreach (var chance in voices.Values)
        {
            total += Math.Max(1, chance);
        }
        return total;
    }

    private ProtoId<TTSVoicePrototype>? ResolveVoicePrototype(string voiceKey)
    {
        if (_prototypeManager.TryIndex<TTSVoicePrototype>(voiceKey, out _))
        {
            return voiceKey;
        }

        var matchingPrototype = _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>()
            .FirstOrDefault(prototype => prototype.Name == voiceKey);

        return matchingPrototype?.ID;
    }
}
