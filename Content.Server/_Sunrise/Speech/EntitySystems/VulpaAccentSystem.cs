using System.Text.RegularExpressions;
using Content.Server._Sunrise.Speech.Components;
using Content.Server.Speech;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Speech.EntitySystems;

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

        // р => ррр
        message = Regex.Replace(
            input: message,
            "р+",
            _random.Pick(new List<string> { "рр", "ррр" })
        );
        // Р => РРР
        message = Regex.Replace(
            input: message,
            "Р+",
            _random.Pick(new List<string> { "РР", "РРР" })
        );

        args.Message = message;
    }
}
