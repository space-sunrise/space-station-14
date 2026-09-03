using System;
using System.Collections.Generic;
using Content.Shared.Preferences;
using Content.Shared.Traits;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Records;

// Sunrise added start — сводка медицинских ограничений персонажа для досье.
// Берётся из черт категории Disabilities, выбранных в редакторе персонажа, а не
// вводится свободным текстом, чтобы досье не расходилось с реальными чертами персонажа.
public static class RecordTraitSummary
{
    private const string DisabilityCategory = "Disabilities";

    public static string FormatDisabilities(ILocalizationManager loc, IPrototypeManager prototypes, HumanoidCharacterProfile? profile)
    {
        if (profile == null)
            return loc.GetString("records-value-no-data");

        var names = new List<string>();
        foreach (var traitId in profile.TraitPreferences)
        {
            if (!prototypes.TryIndex(traitId, out var trait) ||
                trait.Category is not { } category ||
                category.Id != DisabilityCategory)
            {
                continue;
            }

            names.Add(loc.GetString(trait.Name));
        }

        if (names.Count == 0)
            return loc.GetString("records-medical-restrictions-none");

        names.Sort(StringComparer.CurrentCulture);
        return string.Join(", ", names);
    }
}
// Sunrise added end
