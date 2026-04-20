using Content.Shared._Sunrise.Laws;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CriminalRecords.Systems;

public abstract class SharedSunriseCriminalRecordsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public int CalculateSentence(CriminalCase @case)
    {
        var baseTotal = 0;
        foreach (var lawId in @case.Laws)
        {
            if (_prototypeManager.TryIndex<CorporateLawPrototype>(lawId, out var law))
            {
                baseTotal += law.BaseSentence;
            }
        }

        var multiplier = 1.0f;
        foreach (var circId in @case.Circumstances)
        {
            if (_prototypeManager.TryIndex<CorporateLawPrototype>(circId, out var circ))
            {
                // Multipliers are additive: 1.2 and 0.8 becomes 1.0 (1 + 0.2 - 0.2)
                multiplier += (circ.SentenceMultiplier - 1.0f);
            }
        }

        // Ensure multiplier doesn't go below 0
        multiplier = Math.Max(0, multiplier);

        return (int)Math.Round(baseTotal * multiplier);
    }
}
