using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class VulpaAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VulpaAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, VulpaAccentComponent component, AccentGetEvent args)
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

        args.Message = message;
    }
}
