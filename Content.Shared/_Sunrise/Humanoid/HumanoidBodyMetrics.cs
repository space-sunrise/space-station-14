using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Humanoid;

// Sunrise added start — единый расчёт роста/веса персонажа по слайдерам телосложения.
// Используется редактором персонажа, предпросмотром досье и печатью станционных записей,
// чтобы значения в досье всегда совпадали с тем, что игрок выставил в редакторе, а не
// вводились произвольным текстом.
public static class HumanoidBodyMetrics
{
    public static int GetHeightCm(SpeciesPrototype species, float height)
    {
        var span = species.MaxHeight - species.MinHeight;
        if (MathF.Abs(span) < 0.001f)
            return (int) MathF.Round(species.MinHeightCm);

        var ratio = Math.Clamp((height - species.MinHeight) / span, 0f, 1f);
        return (int) MathF.Round(species.MinHeightCm + (species.MaxHeightCm - species.MinHeightCm) * ratio);
    }

    public static int GetWeightKg(SpeciesPrototype species, float width, float height)
    {
        return (int) MathF.Round(species.StandardWeight + species.StandardDensity * (width * height - 1f));
    }

    public static string FormatHeight(ILocalizationManager loc, SpeciesPrototype? species, float height)
    {
        return species == null
            ? loc.GetString("records-value-no-data")
            : loc.GetString("records-height-value", ("value", GetHeightCm(species, height)));
    }

    public static string FormatWeight(ILocalizationManager loc, SpeciesPrototype? species, float width, float height)
    {
        return species == null
            ? loc.GetString("records-value-no-data")
            : loc.GetString("records-weight-value", ("value", GetWeightKg(species, width, height)));
    }

    public static string FormatHeight(ILocalizationManager loc, IPrototypeManager prototypes, string speciesId, HumanoidCharacterProfile? profile)
    {
        return profile != null && prototypes.TryIndex<SpeciesPrototype>(speciesId, out var species)
            ? FormatHeight(loc, species, profile.Height)
            : loc.GetString("records-value-no-data");
    }

    public static string FormatWeight(ILocalizationManager loc, IPrototypeManager prototypes, string speciesId, HumanoidCharacterProfile? profile)
    {
        return profile != null && prototypes.TryIndex<SpeciesPrototype>(speciesId, out var species)
            ? FormatWeight(loc, species, profile.Width, profile.Height)
            : loc.GetString("records-value-no-data");
    }
}
// Sunrise added end
