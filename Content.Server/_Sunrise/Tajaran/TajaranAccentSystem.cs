using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class TajaranAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TajaranAccentComponent, AccentGetEvent>(OnAccent);
        }

        private void OnAccent(EntityUid uid, TajaranAccentComponent component, AccentGetEvent args)
        {
            var message = args.Message;

            // r => rrr
            message = Regex.Replace(
                message,
                "r+",
                _random.Pick(new List<string> { "rr", "rrr" })
            );
            // R => RRR
            message = Regex.Replace(
                message,
                "R+",
                _random.Pick(new List<string> { "RR", "RRR" })
            );

            // р => ррр
            message = Regex.Replace(
                message,
                "р+",
                _random.Pick(new List<string> { "рр", "ррр" })
            );
            // Р => РРР
            message = Regex.Replace(
                message,
                "Р+",
                _random.Pick(new List<string> { "РР", "РРР" })
            );

            // Вставка апострофов
            message = AddApostrophes(message);

            args.Message = message;
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
