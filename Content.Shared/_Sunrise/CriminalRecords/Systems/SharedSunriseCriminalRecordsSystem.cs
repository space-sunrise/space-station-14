using System.Linq;
using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.Laws.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CriminalRecords.Systems;

public abstract class SharedSunriseCriminalRecordsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStationCorporateLawSystem _corporateLawSystem = default!;

    public int CalculateSentence(CriminalCase @case, List<CriminalCase> allCases)
    {
        // Use the shared law system for robust resolution with fallbacks
        _corporateLawSystem.GetEffectiveLawset(@case.OriginStation, out var provisions, out var articles, out var circumstances, out var threshold);

        return CalculateSentence(@case, allCases, provisions, circumstances, articles);
    }

    private int CalculateSentence(
        CriminalCase @case,
        List<CriminalCase> allCases,
        List<ProtoId<CorporateLawPrototype>> provisions,
        List<ProtoId<CorporateLawPrototype>> circumstances,
        List<ProtoId<CorporateLawSectionPrototype>> articles)
    {

        // 1. Group articles by section and find maximum for each
        var sectionMaxes = new Dictionary<string, int>();
        int heaviestArticleBase = 0;

        foreach (var lawId in @case.Laws)
        {
            if (!_prototypeManager.TryIndex(lawId, out var law))
                continue;

            // --- FILTER: Check if law is in the provided provisions or articles ---
            bool isValid = provisions.Contains(lawId);
            string sectionId = "unknown";

            foreach (var s in articles)
            {
                if (!_prototypeManager.TryIndex(s, out var section) || !section.Entries.Contains(lawId))
                    continue;
                sectionId = s;
                isValid = true;
                break;
            }

            if (!isValid)
                continue;

            heaviestArticleBase = Math.Max(heaviestArticleBase, law.BaseSentence);

            if (sectionId != "unknown")
            {
                if (!sectionMaxes.TryGetValue(sectionId, out var currentMax) || law.BaseSentence > currentMax)
                    sectionMaxes[sectionId] = law.BaseSentence;
            }
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
            // --- FILTER: Check if circumstance is in the provided list ---
            if (!circumstances.Contains(circId))
                continue;

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
