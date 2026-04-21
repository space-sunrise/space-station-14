using System.Linq;
using Content.Shared._Sunrise.Laws;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CriminalRecords.Systems;

public abstract class SharedSunriseCriminalRecordsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public int CalculateSentence(CriminalCase @case, List<CriminalCase> allCases)
    {
        if (!_prototypeManager.TryIndex<CorporateLawsetPrototype>("StandardCorporateLaw", out var lawset))
            return 0;

        // 1. Group articles by section and find maximum for each
        var sectionMaxes = new Dictionary<string, int>();
        int heaviestArticleBase = 0;

        foreach (var lawId in @case.Laws)
        {
            if (!_prototypeManager.TryIndex<CorporateLawPrototype>(lawId, out var law))
                continue;

            heaviestArticleBase = Math.Max(heaviestArticleBase, law.BaseSentence);

            // Find section
            string sectionId = "unknown";
            foreach (var s in lawset.Articles)
            {
                if (!_prototypeManager.TryIndex<CorporateLawSectionPrototype>(s, out var section) || !section.Entries.Contains(lawId))
                    continue;
                sectionId = s;
                break;
            }

            if (!sectionMaxes.TryGetValue(sectionId, out var currentMax) || law.BaseSentence > currentMax)
                sectionMaxes[sectionId] = law.BaseSentence;
        }

        // 2. Sum up section maxes and apply cap
        int baseSum = sectionMaxes.Values.Sum();
        float cap = heaviestArticleBase * 1.5f;
        int cappedBase = (int) Math.Min(baseSum, cap);

        // 3. Multipliers (Circumstances & Recidivism)
        float multiplierModifier = 0.0f; // Additive part for recidivism
        float multiplierFactor = 1.0f;   // Multiplicative part for circumstances

        // Circumstances
        foreach (var circId in @case.Circumstances)
        {
            if (_prototypeManager.TryIndex<CorporateLawPrototype>(circId, out var law))
                multiplierFactor *= law.SentenceMultiplier;
        }

        // Recidivism: +15% per unique repeating article
        var pastLaws = allCases
            .Where(c => c.Id != @case.Id)
            .SelectMany(c => c.Laws)
            .ToHashSet();

        foreach (var lawId in @case.Laws.Distinct())
        {
            if (pastLaws.Contains(lawId))
                multiplierModifier += 0.15f;
        }

        return (int) Math.Round(cappedBase * multiplierFactor * (1.0f + multiplierModifier));
    }
}
