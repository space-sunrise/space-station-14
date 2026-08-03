using System.Collections.Generic;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static void AddSunriseLegacyHairMarkings(List<Marking> markings, Profile profile)
    {
        if (CreateSunriseLegacyHairMarking(
                profile.FacialHairName,
                profile.FacialHairColor,
                profile.FacialHairColorType,
                profile.FacialHairExtendedColor) is { } facialHair)
        {
            markings.Add(facialHair);
        }

        if (CreateSunriseLegacyHairMarking(
                profile.HairName,
                profile.HairColor,
                profile.HairColorType,
                profile.HairExtendedColor) is { } hair)
        {
            markings.Add(hair);
        }
    }

    private static Marking? CreateSunriseLegacyHairMarking(
        string? markingId,
        string? colorHex,
        int effectType,
        string? serializedEffect)
    {
        if (string.IsNullOrWhiteSpace(markingId))
            return null;

        var color = ParseSunriseLegacyColor(colorHex, Color.Black);
        var effects = MarkingEffectCompatibility.TryReadLegacyEffect(effectType, serializedEffect, color, out var effect)
            ? new List<MarkingEffect> { effect }
            : null;

        return new Marking(markingId, new List<Color> { color }, effects);
    }

    private static void ApplySunriseLegacyHairEffects(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        Profile profile)
    {
        TryApplySunriseLegacyHairEffect(
            markings,
            HumanoidVisualLayers.Hair,
            profile.HairColor,
            profile.HairColorType,
            profile.HairExtendedColor);

        TryApplySunriseLegacyHairEffect(
            markings,
            HumanoidVisualLayers.FacialHair,
            profile.FacialHairColor,
            profile.FacialHairColorType,
            profile.FacialHairExtendedColor);
    }

    private static void StoreSunriseHairEffects(Profile profile, Marking? hair, Marking? facialHair)
    {
        var hairEffect = hair?.MarkingEffects is { Count: > 0 } hairEffects
            ? hairEffects[0]
            : null;
        var facialHairEffect = facialHair?.MarkingEffects is { Count: > 0 } facialHairEffects
            ? facialHairEffects[0]
            : null;

        profile.HairColorType = (int)(hairEffect?.Type ?? MarkingEffectType.Color);
        profile.HairExtendedColor = hairEffect?.ToString() ?? string.Empty;
        profile.FacialHairColorType = (int)(facialHairEffect?.Type ?? MarkingEffectType.Color);
        profile.FacialHairExtendedColor = facialHairEffect?.ToString() ?? string.Empty;
    }

    private static void TryApplySunriseLegacyHairEffect(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        HumanoidVisualLayers layer,
        string? colorHex,
        int effectType,
        string? serializedEffect)
    {
        var color = ParseSunriseLegacyColor(colorHex, Color.Black);
        if (!MarkingEffectCompatibility.TryReadLegacyEffect(effectType, serializedEffect, color, out var effect))
            return;

        foreach (var organMarkings in markings.Values)
        {
            if (!organMarkings.TryGetValue(layer, out var layerMarkings))
                continue;

            for (var i = 0; i < layerMarkings.Count; i++)
            {
                var marking = layerMarkings[i];
                marking.SetMarkingEffect(0, effect.Clone());
                layerMarkings[i] = marking;
            }
        }
    }

    private static Color ParseSunriseLegacyColor(string? colorHex, Color fallback)
    {
        return string.IsNullOrWhiteSpace(colorHex)
            ? fallback
            : Color.TryFromHex(colorHex) ?? fallback;
    }
}
