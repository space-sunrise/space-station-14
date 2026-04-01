using System.Text.RegularExpressions;
using System.Linq;
using Content.Server._Sunrise.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Speech.EntitySystems;

public sealed class VulpaAccentSystem : EntitySystem
{
    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");
    private static readonly Regex LowerLatinRRegex = new("r+");
    private static readonly Regex UpperLatinRRegex = new("R+");
    private static readonly Regex LowerCyrillicRRegex = new("р+");
    private static readonly Regex UpperCyrillicRRegex = new("Р+");
    private static readonly IReadOnlyList<string> VulpaWords =
    [
        "Гав",
        "Вуф",
        "Арф",
        "Гррф",
        "Авоо",
    ];

    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VulpaAccentComponent, AccentGetEvent>(OnAccent, after: [typeof(OwOAccentSystem)]);
    }

    public string Accentuate(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return _random.Pick(VulpaWords).Trim();

        message = LowerLatinRRegex.Replace(message, _random.Pick(new List<string> { "rr", "rrr" }));
        message = UpperLatinRRegex.Replace(message, _random.Pick(new List<string> { "RR", "RRR" }));
        message = LowerCyrillicRRegex.Replace(message, _random.Pick(new List<string> { "рр", "ррр" }));
        message = UpperCyrillicRRegex.Replace(message, _random.Pick(new List<string> { "РР", "РРР" }));

        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(message).Value.Any(char.IsLower);
        var vulpaWord = _random.Pick(VulpaWords);

        if (!firstWordAllCaps)
            message = message[0].ToString().ToUpperInvariant() + message[1..];

        return vulpaWord + "... " + message;
    }

    private void OnAccent(EntityUid uid, VulpaAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
