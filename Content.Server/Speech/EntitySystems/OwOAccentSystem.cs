using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class OwOAccentSystem : EntitySystem
    {
        private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "you", "wu" },
            { "You", "Wu"},
            { "cute", "kawaii" },
            { "Cute", "Kawaii" },
            { "cat", "kitty" },
            { "Cat", "Kitty" },
            { "kiss", "mwah" },
            { "Kiss", "Mwah" },
            { "good", "guwd" },
            { "Good", "Guwd" },
            { "no", "nuu" },
            { "No", "Nuu" },
            { "ты", "ти" }, // Russian-Localization
            { "Ты", "Ти" },
            { "маленький", "мавенки" },
            { "Маленький", "Мавенки" },
        };


        public override void Initialize()
        {
            SubscribeLocalEvent<OwOAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            return message
                // Russian-Localization-Start
                .Replace("р", "в").Replace("Р", "В")
                .Replace("л", "в").Replace("Л", "В")
                // Russian-Localization-End
                .Replace("r", "w").Replace("R", "W")
                .Replace("l", "w").Replace("L", "W");
        }

        private void OnAccent(EntityUid uid, OwOAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
