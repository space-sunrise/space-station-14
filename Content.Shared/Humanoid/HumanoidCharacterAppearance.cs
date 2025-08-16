using System.Linq;
using Content.Shared._Sunrise.MarkingEffects;
using System.Numerics;
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

    // sunrise gradient edit start

    [DataField]
    public MarkingEffectType HairMarkingEffectType { get; set; } = MarkingEffectType.Color;

    [DataField]
    public MarkingEffect? HairMarkingEffect { get; set; }

    [DataField]
    public MarkingEffectType FacialHairMarkingEffectType { get; set; } = MarkingEffectType.Color;

    [DataField]
    public MarkingEffect? FacialHairMarkingEffect { get; set; }

    // sunrise gradient edit end

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Humanoid.SkinColor.ValidHumanSkinTone;

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    [DataField]
    public float Width { get; set; } = 1f; //Sunrise

    [DataField]
    public float Height { get; set; } = 1f; //Sunrise

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
        //sunrise gradient end
        float width, //Sunrise
        float height) //Sunrise
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        //sunrise gradient start
        HairMarkingEffectType = hairMarkingEffectType;
        HairMarkingEffect = hairMarkingEffect;
        FacialHairMarkingEffectType = facialHairMarkingEffectType;
        FacialHairMarkingEffect = facialHairMarkingEffect;
        Width = width; //Sunrise
        Height = height; //Sunrise
        //sunrise gradient end
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
            other.Width,
            other.Height) // sunrise gradient edit
    {

    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor, MarkingEffect? newExtendedColor = null)
    {
        return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, newExtendedColor?.Type ?? HairMarkingEffectType, newExtendedColor ?? HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height);
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor, MarkingEffect? newFacialExtendedColor = null)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, newFacialExtendedColor?.Type ?? FacialHairMarkingEffectType, newFacialExtendedColor ?? FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }

    // sunrise gradient edit start
    public HumanoidCharacterAppearance WithHairExtendedColor(MarkingEffect? newExtendedColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, newExtendedColor?.Type ?? MarkingEffectType.Color, newExtendedColor, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, Height); // sunrise gradient edit
    }
    public HumanoidCharacterAppearance WithFacialHairExtendedColor(MarkingEffect? newFacialExtendedColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, newFacialExtendedColor?.Type ?? MarkingEffectType.Color, newFacialExtendedColor, Width, Height); // sunrise gradient edit
    }
    // sunrise gradient edit end

    public HumanoidCharacterAppearance WithWidth(float newWidth)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, newWidth, Height);
    }

    public HumanoidCharacterAppearance WithHeight(float newHeight)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, HairMarkingEffectType, HairMarkingEffect, FacialHairMarkingEffectType, FacialHairMarkingEffect, Width, newHeight);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);

        var skinColor = speciesPrototype.SkinColoration switch
        {
            HumanoidSkinColor.HumanToned => Humanoid.SkinColor.HumanSkinTone(speciesPrototype.DefaultHumanSkinTone),
            HumanoidSkinColor.Hues => speciesPrototype.DefaultSkinTone,
            HumanoidSkinColor.TintedHues => Humanoid.SkinColor.TintedHues(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.VoxFeathers => Humanoid.SkinColor.ClosestVoxColor(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.None => Color.Transparent, // Sunrise-edit
            _ => Humanoid.SkinColor.ValidHumanSkinTone,
        };

        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            new (),
            // sunrise gradient edit start
            MarkingEffectType.Color,
            null,
            MarkingEffectType.Color,
            null,
            // sunrise gradient edit end
            speciesPrototype.DefaultWidth, //Sunrise
            speciesPrototype.DefaultHeight //Sunrise
        );
    }

    private static IReadOnlyList<Color> RealisticEyeColors = new List<Color>
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
        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
        var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

        var newHairStyle = hairStyles.Count > 0
            ? random.Pick(hairStyles)
            : HairStyles.DefaultHairStyle.Id;

        var newFacialHairStyle = facialHairStyles.Count == 0 || sex == Sex.Female
            ? HairStyles.DefaultFacialHairStyle.Id
            : random.Pick(facialHairStyles);

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        // TODO: Add random markings

        var newEyeColor = random.Pick(RealisticEyeColors);

        var skinType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).SkinColoration;

        var newSkinColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);
        switch (skinType)
        {
            case HumanoidSkinColor.HumanToned:
                newSkinColor = Humanoid.SkinColor.HumanSkinTone(random.Next(0, 101));
                break;
            case HumanoidSkinColor.Hues:
                break;
            case HumanoidSkinColor.TintedHues:
                newSkinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(newSkinColor);
                break;
            case HumanoidSkinColor.VoxFeathers:
                newSkinColor = Humanoid.SkinColor.ProportionalVoxColor(newSkinColor);
                break;
            case HumanoidSkinColor.None: // Sunrise-edit
                newSkinColor = Color.Transparent;
                break;
        }

        //Sunrise start
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var newWidth = random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        var newHeight = random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        //Sunrise end

        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, new (), MarkingEffectType.Color, null, MarkingEffectType.Color, null, newWidth, newHeight);

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
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

        var width = appearance.Width; //Sunrise
        var height = appearance.Height; //Sunrise

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        // Sunrise-Sponsors-Start
        if (proto.TryIndex(hairStyleId, out MarkingPrototype? hairProto) &&
            hairProto.SponsorOnly &&
            !sponsorPrototypes.Contains(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }
        // Sunrise-Sponsors-End

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }

        // Sunrise-Sponsors-Start
        if (proto.TryIndex(facialHairStyleId, out MarkingPrototype? facialHairProto) &&
            facialHairProto.SponsorOnly &&
            !sponsorPrototypes.Contains(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }
        // Sunrise-Sponsors-End

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            if (!Humanoid.SkinColor.VerifySkinColor(speciesProto.SkinColoration, skinColor))
            {
                skinColor = Humanoid.SkinColor.ValidSkinTone(speciesProto.SkinColoration, skinColor);
            }

            width = Math.Clamp(width, speciesProto.MinWidth, speciesProto.MaxWidth); // Sunrise
            height = Math.Clamp(height, speciesProto.MinHeight, speciesProto.MaxHeight); // Sunrise

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
            markingSet.FilterSponsor(sponsorPrototypes, markingManager); // Sunrise-Sponsors
        }

        // sunrise gradient start
        MarkingEffect? hairExtendedColor = null;
        if (appearance.HairMarkingEffect != null)
        {
            hairExtendedColor = appearance.HairMarkingEffect;
            foreach (var (key, value) in hairExtendedColor.Colors)
                hairExtendedColor.Colors[key] = ClampColor(value);
        }

        MarkingEffect? facialHairExtendedColor = null;
        if (appearance.FacialHairMarkingEffect != null)
        {
            facialHairExtendedColor = appearance.FacialHairMarkingEffect;
            foreach (var (key, value) in facialHairExtendedColor.Colors)
                facialHairExtendedColor.Colors[key] = ClampColor(value);
        }
        // sunrise gradient end

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
        // sunrise gradient edit start
        if (!HairMarkingEffectType.Equals(other.HairMarkingEffectType)) return false;
        if (!Equals(HairMarkingEffect, other.HairMarkingEffect)) return false;
        if (!FacialHairMarkingEffectType.Equals(other.FacialHairMarkingEffectType)) return false;
        if (!Equals(FacialHairMarkingEffect, other.FacialHairMarkingEffect)) return false;
        // sunrise gradient edit end
        if (Width != other.Width) return false; //Sunrise
        if (Height != other.Height) return false; //Sunrise
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
               HairMarkingEffectType.Equals(other.HairMarkingEffectType) &&
               Equals(HairMarkingEffect, other.HairMarkingEffect) &&
               FacialHairMarkingEffectType.Equals(other.FacialHairMarkingEffectType) &&
               Equals(FacialHairMarkingEffect, other.FacialHairMarkingEffect) &&
               Width == other.Width && //starlight
               Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, new Vector2(Width, Height));
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
