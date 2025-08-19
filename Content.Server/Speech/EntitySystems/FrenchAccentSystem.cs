using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// System that gives the speaker a faux-French accent.
/// </summary>
public sealed class FrenchAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexK = new(@"к"); // Sunrise-Edit строчная
    private static readonly Regex RegexR = new(@"р"); // Sunrise-Edit
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, FrenchAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");

        // replaces к with кх at the start of words.
        msg = Regex.Replace(msg, @"[кК]", m => m.Value == "К" ? "КХ" : "кх"); // Sunrise-Edit

        // replaces р with х at the start of words.
        msg = Regex.Replace(msg, @"[рР]", m => m.Value == "Р" ? "Х" : "х"); // Sunrise-Edit

        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");

        return msg;
    }

    private void OnAccentGet(EntityUid uid, FrenchAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
