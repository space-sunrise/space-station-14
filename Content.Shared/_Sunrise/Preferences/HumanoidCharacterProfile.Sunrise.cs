using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static readonly Regex RestrictedNameRegex = new("[^А-Яа-яA-Za-zёЁ0-9, ,\\-,'.]");

    public HumanoidCharacterProfile WithVoice(string voice)
    {
        return new(this) { Voice = voice };
    }

    private static string PickRandomVoiceSunrise(IPrototypeManager prototypeManager, IRobustRandom random, Sex sex)
    {
        return random.Pick(prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => CanHaveVoice(o, sex) && !o.SponsorOnly)
            .ToArray()
        ).ID;
    }

    private static string RestrictNameSunrise(string name, IConfigurationManager configManager)
    {
        if (!configManager.GetCVar(CCVars.RestrictedNames))
            return name;

        return RestrictedNameRegex.Replace(name, string.Empty);
    }

    private int GetMaxFlavorLengthSunrise(ICommonSession session, IConfigurationManager configManager)
    {
        IoCManager.Instance!.TryResolveType<ISharedSponsorsManager>(out var sponsors);
        var maxDescLength = configManager.GetCVar(SunriseCCVars.FlavorTextBaseLength);

        if (sponsors == null)
            return maxDescLength;

        if (sponsors.IsSponsor(session.UserId))
            maxDescLength = sponsors.GetSizeFlavor(session.UserId);

        if (!sponsors.IsAllowedFlavor(session.UserId) && configManager.GetCVar(SunriseCCVars.FlavorTextSponsorOnly))
            FlavorText = string.Empty;

        return maxDescLength;
    }

    private void EnsureVoiceValidSunrise(IPrototypeManager prototypeManager, Sex sex)
    {
        prototypeManager.TryIndex<TTSVoicePrototype>(Voice, out var voice);
        if (voice is null || !CanHaveVoice(voice, Sex))
            Voice = SharedHumanoidAppearanceSystem.DefaultSexVoice[sex];
    }

    public static bool CanHaveVoice(TTSVoicePrototype voice, Sex sex)
    {
        return voice.RoundStart && sex == Sex.Unsexed || (voice.Sex == sex || voice.Sex == Sex.Unsexed);
    }
}
