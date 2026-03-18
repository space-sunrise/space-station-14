using System.Linq;
using System.Numerics;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField("hair")]
    public string HairStyleId { get; set; } = HairStyles.DefaultHairStyle;

    [DataField]
    public Color HairColor { get; set; } = Color.Black;

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings,
        //sunrise gradient start
        MarkingEffectType hairMarkingEffectType,
        MarkingEffect? hairMarkingEffect,
        MarkingEffectType facialHairMarkingEffectType,
        MarkingEffect? facialHairMarkingEffect,
        // Sunrise - Start
        float width,
        float height)
        : this(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markings,
            hairMarkingEffectType,
            hairMarkingEffect,
            facialHairMarkingEffectType,
            facialHairMarkingEffect,
            false,
            width,
            height)
    {
    }

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings,
        MarkingEffectType hairMarkingEffectType,
        MarkingEffect? hairMarkingEffect,
        MarkingEffectType facialHairMarkingEffectType,
        MarkingEffect? facialHairMarkingEffect,

        bool hairMirrored,
        float width,
        float height)
        // Sunrise - End
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        InitializeAppearanceSunrise(hairMarkingEffectType, hairMarkingEffect,
        facialHairMarkingEffectType, facialHairMarkingEffect, hairMirrored, width, height); // Sunrise-edit
    }

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings,
        float width,
        float height,
        bool hairGradientEnabled = false,
        Color hairGradientSecondaryColor = default,
        int hairGradientDirection = 0,
        bool facialHairGradientEnabled = false,
        Color facialHairGradientSecondaryColor = default,
        int facialHairGradientDirection = 0,
        bool allMarkingsGradientEnabled = false,
        Color allMarkingsGradientSecondaryColor = default,
        int allMarkingsGradientDirection = 0,
        // Sunrise - Start
        bool hairMirrored = false)
        // Sunrise - End
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        InitializeAppearanceSizeSunrise(hairMirrored, width, height); // Sunrise-edit
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId,
            other.HairColor,
            other.FacialHairStyleId,
            other.FacialHairColor,
            other.EyeColor,
            other.SkinColor,
            new(other.Markings),
            other.HairMarkingEffectType,
            other.HairMarkingEffect,
            other.FacialHairMarkingEffectType,
            other.FacialHairMarkingEffect,
            // Sunrise - Start
            other.HairMirrored,
            // Sunrise - End
            other.Width,
            other.Height) // sunrise gradient edit
    {

    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return CreateAppearanceWithSunrise(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings); // Sunrise-edit
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor, MarkingEffect? newExtendedColor = null)
    {
        return CreateAppearanceWithSunrise(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, hairMarkingEffectType: newExtendedColor?.Type ?? HairMarkingEffectType, hairMarkingEffect: newExtendedColor ?? HairMarkingEffect); // Sunrise-edit
    }

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return CreateAppearanceWithSunrise(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings); // Sunrise-edit
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor, MarkingEffect? newFacialExtendedColor = null)
    {
        return CreateAppearanceWithSunrise(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings, facialHairMarkingEffectType: newFacialExtendedColor?.Type ?? FacialHairMarkingEffectType, facialHairMarkingEffect: newFacialExtendedColor ?? FacialHairMarkingEffect); // Sunrise-edit
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return CreateAppearanceWithSunrise(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings); // Sunrise-edit
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return CreateAppearanceWithSunrise(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings); // Sunrise-edit
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return CreateAppearanceWithSunrise(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings); // Sunrise-edit
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        return CreateDefaultAppearanceSunrise(skinColor, speciesPrototype); // Sunrise-edit
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var newFacialHairStyle = HairStyles.DefaultFacialHairStyle;
        var newHairStyle = HairStyles.DefaultHairStyle;
        List<Marking> newMarkings = [];

        PickRandomHairStylesSunrise(markingManager, random, species, sex, ref newHairStyle, ref newFacialHairStyle); // Sunrise-edit

        // grab a completely random color.
        var baseColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);

        // create a new color palette based on BaseColor. roll to determine what type of palette it is.
        // personally I think this should be weighted, but I can't be bothered to implement that.
        List<Color> colorPalette = [];
        switch (random.Next(3))
        {
            case 0:
                colorPalette = GetSplitComplementaries(baseColor);
                break;
            case 1:
                colorPalette = GetTriadicComplementaries(baseColor);
                break;
            case 2:
                colorPalette = GetOneComplementary(baseColor);
                break;
        }

        var newHairColor = colorPalette[1];
        var newEyeColor = colorPalette[2];

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        // and pick a random realistic eye color from the list.
        newEyeColor = random.Pick(_realisticEyeColors);

        var (newWidth, newHeight) = GetRandomSizeSunrise(species, random); // Sunrise-edit

        return CreateRandomAppearanceSunrise(newHairStyle, newHairColor, newFacialHairStyle, newEyeColor, newSkinColor, newWidth, newHeight); // Sunrise-edit

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }

        List<Color> GetComplementaryColors(Color color, double angle)
        {
            var hsl = Color.ToHsl(color);

            var hVal = hsl.X + angle;
            hVal = hVal >= 0.360 ? hVal - 0.360 : hVal;
            var positiveHSL = new Vector4((float)hVal, hsl.Y, hsl.Z, hsl.W);

            var hVal1 = hsl.X - angle;
            hVal1 = hVal1 <= 0 ? hVal1 + 0.360 : hVal1;
            var negativeHSL = new Vector4((float)hVal1, hsl.Y, hsl.Z, hsl.W);

            var c0 = Color.FromHsl(positiveHSL);
            var c1 = Color.FromHsl(negativeHSL);

            var palette = new List<Color> { color, c0, c1 };
            return palette;
        }

        // return a list of triadic complementary colors
        List<Color> GetTriadicComplementaries(Color color)
        {
            return GetComplementaryColors(color, 0.120);
        }

        // return a list of split complementary colors
        List<Color> GetSplitComplementaries(Color color)
        {
            return GetComplementaryColors(color, 0.150);
        }

        // return a list containing the base color and two copies of a single complemenary color
        List<Color> GetOneComplementary(Color color)
        {
            return GetComplementaryColors(color, 0.180);
        }

        Color SquashToSkinLuminosity(Color skinColor, Color toSquash)
        {
            var skinColorHSL = Color.ToHsl(skinColor);
            var toSquashHSL = Color.ToHsl(toSquash);

            // check if the skin color is as dark as or darker than the marking color:
            if (toSquashHSL.Z <= skinColorHSL.Z)
            {
                // if it is, don't fuck with it
                return toSquash;
            }

            // otherwise, create a new color with the H, S, and A of toSquash, but the L of skinColor
            var newColor = new Vector4(toSquashHSL.X, toSquashHSL.Y, skinColorHSL.Z, toSquashHSL.W);
            return Color.FromHsl(newColor);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex, string[] sponsorPrototypes)
    {
        var hairStyleId = appearance.HairStyleId;
        var facialHairStyleId = appearance.FacialHairStyleId;

        var hairColor = ClampColor(appearance.HairColor);
        var facialHairColor = ClampColor(appearance.FacialHairColor);
        var eyeColor = ClampColor(appearance.EyeColor);

        var width = appearance.Width;
        var height = appearance.Height;

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }

        EnsureSponsorStylesSunrise(proto, sponsorPrototypes, ref hairStyleId, ref facialHairStyleId); // Sunrise-edit

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            skinColor = strategy.EnsureVerified(skinColor);

            (width, height) = ClampSizeSunrise(width, height, speciesProto); // Sunrise-edit

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
            markingSet.FilterSponsor(sponsorPrototypes, markingManager); // Sunrise-Sponsors
        }

        // Sunrise-start
        var hairExtendedColor = CloneEffectSunrise(appearance.HairMarkingEffect);
        var facialHairExtendedColor = CloneEffectSunrise(appearance.FacialHairMarkingEffect);
        // Sunrise-end

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList(),
            appearance.HairMarkingEffectType,
            hairExtendedColor,
            appearance.FacialHairMarkingEffectType,
            facialHairExtendedColor,
            appearance.HairMirrored, // Sunrise-edit
            width,
            height);
    }
    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (HairStyleId != other.HairStyleId) return false;
        if (!HairColor.Equals(other.HairColor)) return false;
        if (FacialHairStyleId != other.FacialHairStyleId) return false;
        if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!Markings.SequenceEqual(other.Markings)) return false;
        if (!MemberwiseEqualsSunrise(other)) return false; // Sunrise-edit
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return HairStyleId == other.HairStyleId &&
               HairColor.Equals(other.HairColor) &&
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings) &&
               // sunrise gradient edit start
               EqualsSunrise(other); // Sunrise-edit
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, GetHashCodeSunrise()); // Sunrise-edit
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
