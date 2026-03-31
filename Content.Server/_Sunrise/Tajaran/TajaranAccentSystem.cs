using System.Text.RegularExpressions;
using System.Linq;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class TajaranAccentSystem : EntitySystem
    {
        private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");
        private static readonly Regex LowerLatinRRegex = new("r+");
        private static readonly Regex UpperLatinRRegex = new("R+");
        private static readonly Regex LowerCyrillicRRegex = new("р+");
        private static readonly Regex UpperCyrillicRRegex = new("Р+");
        private static readonly IReadOnlyList<string> TajaranWords =
        [
            "Мрр...",
            "Мяу...",
            "Нья...",
            "Пурр...",
            "Ррр...",
        ];

        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TajaranAccentComponent, AccentGetEvent>(OnAccent, after: [typeof(OwOAccentSystem)]);
        }

        public string Accentuate(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return _random.Pick(TajaranWords).Trim();

            // r => rrr
            message = LowerLatinRRegex.Replace(message, _random.Pick(new List<string> { "rr", "rrr" }));
            // R => RRR
            message = UpperLatinRRegex.Replace(message, _random.Pick(new List<string> { "RR", "RRR" }));

            // р => ррр
            message = LowerCyrillicRRegex.Replace(message, _random.Pick(new List<string> { "рр", "ррр" }));
            // Р => РРР
            message = UpperCyrillicRRegex.Replace(message, _random.Pick(new List<string> { "РР", "РРР" }));

            // Вставка апострофов
            message = AddApostrophes(message);


            var firstWordAllCaps = !FirstWordAllCapsRegex.Match(message).Value.Any(char.IsLower);
            var tajaranWord = _random.Pick(TajaranWords);

            if (!firstWordAllCaps)
                message = message[0].ToString().ToUpperInvariant() + message[1..];
            else
                tajaranWord = tajaranWord.ToUpperInvariant();

            return tajaranWord + " " + message;
        }

        private void OnAccent(EntityUid uid, TajaranAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }

        private string AddApostrophes(string message)
        {
            var words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 6 && _random.NextDouble() < 0.3)
                {
                    var index = _random.Next(1, words[i].Length - 1);
                    words[i] = words[i].Insert(index, "'");
                }
            }
            return string.Join(' ', words);
        }
    }
}
