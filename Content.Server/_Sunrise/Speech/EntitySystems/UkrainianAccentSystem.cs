using System.Text;
using System.Text.RegularExpressions;
using Content.Server._Sunrise.Speech.Components;
using Content.Server._Sunrise.TTS;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Sunrise.Speech.EntitySystems;

public sealed class UkrainianAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<UkrainianAccentComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<UkrainianAccentComponent, TTSSanitizeEvent>(OnSanitize);
    }

    private string Accentuate(string message)
    {
        var accentedMessage = new StringBuilder(_replacement.ApplyReplacements(message, "ukrainian"));

        for (var i = 0; i < accentedMessage.Length; i++)
        {
            var c = accentedMessage[i];

            accentedMessage[i] = c switch
            {
                'и' => 'і',
                'И' => 'І',
                'ы' => 'и',
                'Ы' => 'И',
                'ё' => 'ї',
                'Ё' => 'Ї',
                'е' => 'є',
                'Е' => 'Є',
                _ => accentedMessage[i]
            };
        }

        return accentedMessage.ToString();
    }

    private void OnAccent(EntityUid uid, UkrainianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }

    private void OnSanitize(EntityUid uid, UkrainianAccentComponent component, TTSSanitizeEvent args)
    {
        var text = args.Text.Trim();
        text = Regex.Replace(text, "[іІ]", "[иИ]");
        text = Regex.Replace(text, "[їЇ]", "[ёЁ]");
        text = Regex.Replace(text, "[єЄ]", "[еЕ]");
        text = Regex.Replace(text, "[ґҐ]", "[гГ]");
        text = Regex.Replace(text, "[еЕ]", "[эЭ]");
        text = text.Trim();
        args.Text = text;
    }
}
