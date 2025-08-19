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

    private static readonly Regex RegexK = new(@"[кК]", RegexOptions.Compiled); // Sunrise-Edit: global, case-aware
    private static readonly Regex RegexR = new(@"[рР]", RegexOptions.Compiled); // Sunrise-Edit: global, case-aware
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

        // replaces 'к/К' with 'кх/КХ' globally (preserves case).
        msg = RegexK.Replace(msg, m => m.Value == "К" ? "КХ" : "кх"); // Sunrise-Edit

        // replaces 'р/Р' with 'х/Х' globally (preserves case).
        msg = RegexR.Replace(msg, m => m.Value == "Р" ? "Х" : "х"); // Sunrise-Edit

        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");

        return msg;
    }

    private void OnAccentGet(EntityUid uid, FrenchAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
