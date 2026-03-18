using System.Linq;
using System.Numerics;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    [DataField]
    public MarkingEffectType HairMarkingEffectType { get; set; } = MarkingEffectType.Color;

    [DataField]
    public MarkingEffect? HairMarkingEffect { get; set; }

    [DataField]
    public MarkingEffectType FacialHairMarkingEffectType { get; set; } = MarkingEffectType.Color;

    [DataField]
    public MarkingEffect? FacialHairMarkingEffect { get; set; }

    [DataField]
    public bool HairMirrored { get; set; }

    [DataField]
    public float Width { get; set; } = 1f;

    [DataField]
    public float Height { get; set; } = 1f;

    private void InitializeAppearanceSunrise(
        MarkingEffectType hairMarkingEffectType,
        MarkingEffect? hairMarkingEffect,
        MarkingEffectType facialHairMarkingEffectType,
        MarkingEffect? facialHairMarkingEffect,
        bool hairMirrored,
        float width,
        float height)
    {
        HairMarkingEffectType = hairMarkingEffectType;
        HairMarkingEffect = hairMarkingEffect;
        FacialHairMarkingEffectType = facialHairMarkingEffectType;
        FacialHairMarkingEffect = facialHairMarkingEffect;
        HairMirrored = hairMirrored;
        Width = width;
        Height = height;
    }

    private void InitializeAppearanceSizeSunrise(bool hairMirrored, float width, float height)
    {
        HairMirrored = hairMirrored;
        Width = width;
        Height = height;
    }

    private HumanoidCharacterAppearance CreateAppearanceWithSunrise(
        string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings,
        MarkingEffectType? hairMarkingEffectType = null,
        MarkingEffect? hairMarkingEffect = null,
        MarkingEffectType? facialHairMarkingEffectType = null,
        MarkingEffect? facialHairMarkingEffect = null,
        bool? hairMirrored = null,
        float? width = null,
        float? height = null)
    {
        return new(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markings,
            hairMarkingEffectType ?? HairMarkingEffectType,
            hairMarkingEffect ?? HairMarkingEffect,
            facialHairMarkingEffectType ?? FacialHairMarkingEffectType,
            facialHairMarkingEffect ?? FacialHairMarkingEffect,
            hairMirrored ?? HairMirrored,
            width ?? Width,
            height ?? Height);
    }

    public HumanoidCharacterAppearance WithHairExtendedColor(MarkingEffect? newExtendedColor)
    {
        return CreateAppearanceWithSunrise(
            HairStyleId,
            HairColor,
            FacialHairStyleId,
            FacialHairColor,
            EyeColor,
            SkinColor,
            Markings,
            hairMarkingEffectType: newExtendedColor?.Type ?? MarkingEffectType.Color,
            hairMarkingEffect: newExtendedColor);
    }

    public HumanoidCharacterAppearance WithFacialHairExtendedColor(MarkingEffect? newFacialExtendedColor)
    {
        return CreateAppearanceWithSunrise(
            HairStyleId,
            HairColor,
            FacialHairStyleId,
            FacialHairColor,
            EyeColor,
            SkinColor,
            Markings,
            facialHairMarkingEffectType: newFacialExtendedColor?.Type ?? MarkingEffectType.Color,
            facialHairMarkingEffect: newFacialExtendedColor);
    }

    public HumanoidCharacterAppearance WithHairMirroring(bool mirrored)
    {
        return CreateAppearanceWithSunrise(
            HairStyleId,
            HairColor,
            FacialHairStyleId,
            FacialHairColor,
            EyeColor,
            SkinColor,
            Markings,
            hairMirrored: mirrored);
    }

    public HumanoidCharacterAppearance WithWidth(float newWidth)
    {
        return CreateAppearanceWithSunrise(
            HairStyleId,
            HairColor,
            FacialHairStyleId,
            FacialHairColor,
            EyeColor,
            SkinColor,
            Markings,
            width: newWidth);
    }

    public HumanoidCharacterAppearance WithHeight(float newHeight)
    {
        return CreateAppearanceWithSunrise(
            HairStyleId,
            HairColor,
            FacialHairStyleId,
            FacialHairColor,
            EyeColor,
            SkinColor,
            Markings,
            height: newHeight);
    }

    private static HumanoidCharacterAppearance CreateDefaultAppearanceSunrise(Color skinColor, SpeciesPrototype speciesPrototype)
    {
        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            [],
            MarkingEffectType.Color,
            null,
            MarkingEffectType.Color,
            null,
            false,
            speciesPrototype.DefaultWidth,
            speciesPrototype.DefaultHeight);
    }

    private static void PickRandomHairStylesSunrise(
        MarkingManager markingManager,
        IRobustRandom random,
        string species,
        Sex sex,
        ref ProtoId<MarkingPrototype> newHairStyle,
        ref ProtoId<MarkingPrototype> newFacialHairStyle)
    {
        var hairStyles = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.Hair, species, sex);
        if (hairStyles.Count > 0)
            newHairStyle = random.Pick(hairStyles.Keys.ToArray());

        if (sex == Sex.Female)
            return;

        var facialHairStyles = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.FacialHair, species, sex);
        if (facialHairStyles.Count > 0)
            newFacialHairStyle = random.Pick(facialHairStyles.Keys.ToArray());
    }

    private static (float Width, float Height) GetRandomSizeSunrise(string species, IRobustRandom random)
    {
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        return (
            random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth),
            random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight));
    }

    private static HumanoidCharacterAppearance CreateRandomAppearanceSunrise(
        string newHairStyle,
        Color newHairColor,
        string newFacialHairStyle,
        Color newEyeColor,
        Color newSkinColor,
        float newWidth,
        float newHeight)
    {
        return new(
            newHairStyle,
            newHairColor,
            newFacialHairStyle,
            newHairColor,
            newEyeColor,
            newSkinColor,
            [],
            MarkingEffectType.Color,
            null,
            MarkingEffectType.Color,
            null,
            false,
            newWidth,
            newHeight);
    }

    private static void EnsureSponsorStylesSunrise(
        IPrototypeManager proto,
        string[] sponsorPrototypes,
        ref string hairStyleId,
        ref string facialHairStyleId)
    {
        if (proto.TryIndex(hairStyleId, out MarkingPrototype? hairProto) &&
            hairProto.SponsorOnly &&
            !sponsorPrototypes.Contains(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        if (proto.TryIndex(facialHairStyleId, out MarkingPrototype? facialHairProto) &&
            facialHairProto.SponsorOnly &&
            !sponsorPrototypes.Contains(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }
    }

    private static (float Width, float Height) ClampSizeSunrise(float width, float height, SpeciesPrototype speciesProto)
    {
        return (
            Math.Clamp(width, speciesProto.MinWidth, speciesProto.MaxWidth),
            Math.Clamp(height, speciesProto.MinHeight, speciesProto.MaxHeight));
    }

    private static MarkingEffect? CloneEffectSunrise(MarkingEffect? effect)
    {
        if (effect == null)
            return null;

        var clone = effect.Clone();
        foreach (var (key, value) in clone.Colors)
        {
            clone.Colors[key] = ClampColor(value);
        }

        return clone;
    }

    private bool MemberwiseEqualsSunrise(HumanoidCharacterAppearance other)
    {
        if (!HairMarkingEffectType.Equals(other.HairMarkingEffectType))
            return false;

        if (!Equals(HairMarkingEffect, other.HairMarkingEffect))
            return false;

        if (!FacialHairMarkingEffectType.Equals(other.FacialHairMarkingEffectType))
            return false;

        if (!Equals(FacialHairMarkingEffect, other.FacialHairMarkingEffect))
            return false;

        if (HairMirrored != other.HairMirrored)
            return false;

        if (Width != other.Width)
            return false;

        if (Height != other.Height)
            return false;

        return true;
    }

    private bool EqualsSunrise(HumanoidCharacterAppearance other)
    {
        return HairMarkingEffectType.Equals(other.HairMarkingEffectType) &&
               Equals(HairMarkingEffect, other.HairMarkingEffect) &&
               FacialHairMarkingEffectType.Equals(other.FacialHairMarkingEffectType) &&
               Equals(FacialHairMarkingEffect, other.FacialHairMarkingEffect) &&
               HairMirrored == other.HairMirrored &&
               Width == other.Width &&
               Height == other.Height;
    }

    private int GetHashCodeSunrise()
    {
        return HashCode.Combine(HairMirrored, new Vector2(Width, Height));
    }
}
