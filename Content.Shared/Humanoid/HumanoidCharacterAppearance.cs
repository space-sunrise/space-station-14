using System.Linq;
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

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    //Sunrise start Hair/Fur gradient settings
    [DataField]
    public bool HairGradientEnabled { get; set; } = false;

    [DataField]
    public Color HairGradientSecondaryColor { get; set; } = Color.White;

    /// <summary>
    /// 0 = bottom->top, 1 = top->bottom, 2 = left->right, 3 = right->left
    /// </summary>
    [DataField]
    public int HairGradientDirection { get; set; } = 0;

    [DataField]
    public bool FacialHairGradientEnabled { get; set; } = false;

    [DataField]
    public Color FacialHairGradientSecondaryColor { get; set; } = Color.White;

    /// <summary>
    /// 0 = bottom->top, 1 = top->bottom, 2 = left->right, 3 = right->left
    /// </summary>
    [DataField]
    public int FacialHairGradientDirection { get; set; } = 0; //Sunrise end

    // Sunrise start Global gradient for all markings (except skin)
    [DataField]
    public bool AllMarkingsGradientEnabled { get; set; } = false;

    [DataField]
    public Color AllMarkingsGradientSecondaryColor { get; set; } = Color.White;

    /// <summary>
    /// 0 = bottom->top, 1 = top->bottom, 2 = left->right, 3 = right->left
    /// </summary>
    [DataField]
    public int AllMarkingsGradientDirection { get; set; } = 0;
    // Sunrise end

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
        Width = width; //Sunrise
        Height = height; //Sunrise
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
        int allMarkingsGradientDirection = 0)
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        Width = width;
        Height = height;
        HairGradientEnabled = hairGradientEnabled;
        HairGradientSecondaryColor = hairGradientSecondaryColor == default ? Color.White : ClampColor(hairGradientSecondaryColor);
        HairGradientDirection = hairGradientDirection;
        FacialHairGradientEnabled = facialHairGradientEnabled;
        FacialHairGradientSecondaryColor = facialHairGradientSecondaryColor == default ? Color.White : ClampColor(facialHairGradientSecondaryColor);
        FacialHairGradientDirection = facialHairGradientDirection;
        AllMarkingsGradientEnabled = allMarkingsGradientEnabled;
        AllMarkingsGradientSecondaryColor = allMarkingsGradientSecondaryColor == default ? Color.White : ClampColor(allMarkingsGradientSecondaryColor);
        AllMarkingsGradientDirection = allMarkingsGradientDirection;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId,
            other.HairColor,
            other.FacialHairStyleId,
            other.FacialHairColor,
            other.EyeColor,
            other.SkinColor,
            new(other.Markings),
            other.Width,
            other.Height,
            other.HairGradientEnabled,
            other.HairGradientSecondaryColor,
            other.HairGradientDirection,
            other.FacialHairGradientEnabled,
            other.FacialHairGradientSecondaryColor,
            other.FacialHairGradientDirection,
            other.AllMarkingsGradientEnabled,
            other.AllMarkingsGradientSecondaryColor,
            other.AllMarkingsGradientDirection)
    {
    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings, Width, Height) //Sunrise start
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        }; //Sunrise end
    }

    //Sunrise start Hair gradient methods
    public HumanoidCharacterAppearance WithHairGradientEnabled(bool enabled)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = enabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithHairGradientSecondaryColor(Color color)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = ClampColor(color),
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithHairGradientDirection(int direction)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = direction,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithFacialHairGradientEnabled(bool enabled)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = enabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithFacialHairGradientSecondaryColor(Color color)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = ClampColor(color),
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithFacialHairGradientDirection(int direction)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = direction,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithAllMarkingsGradientEnabled(bool enabled)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = enabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithAllMarkingsGradientSecondaryColor(Color color)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = ClampColor(color),
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithAllMarkingsGradientDirection(int direction)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = direction,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = direction
        };
    }

    public HumanoidCharacterAppearance WithWidth(float newWidth)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, newWidth, Height)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }

    public HumanoidCharacterAppearance WithHeight(float newHeight)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings, Width, newHeight)
        {
            HairGradientEnabled = HairGradientEnabled,
            HairGradientSecondaryColor = HairGradientSecondaryColor,
            HairGradientDirection = HairGradientDirection,
            FacialHairGradientEnabled = FacialHairGradientEnabled,
            FacialHairGradientSecondaryColor = FacialHairGradientSecondaryColor,
            FacialHairGradientDirection = FacialHairGradientDirection,
            AllMarkingsGradientEnabled = AllMarkingsGradientEnabled,
            AllMarkingsGradientSecondaryColor = AllMarkingsGradientSecondaryColor,
            AllMarkingsGradientDirection = AllMarkingsGradientDirection
        };
    }
    //Sunrise end

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

        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            new(),
            speciesPrototype.DefaultWidth, //Sunrise
            speciesPrototype.DefaultHeight //Sunrise
        );
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

        // Sunrise - Start
        var hairStyles = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.Hair, species, sex);
        if (hairStyles.Count > 0)
            newHairStyle = random.Pick(hairStyles.Keys.ToArray());

        if (sex != Sex.Female)
        {
            var facialHairStyles = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.FacialHair, species, sex);
            if (facialHairStyles.Count > 0)
                newFacialHairStyle = random.Pick(facialHairStyles.Keys.ToArray());
        }
        // Sunrise - End

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

        //Sunrise start
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var newWidth = random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        var newHeight = random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        //Sunrise end

        // at the end of all that, we should have new values for each of these, so we set the character appearance to these new values.
        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, newMarkings, newWidth, newHeight); // Sunrise edit

        // helper functions:
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

        var hairGradientEnabled = appearance.HairGradientEnabled;
        var hairGradientSecondaryColor = ClampColor(appearance.HairGradientSecondaryColor);
        var hairGradientDirection = appearance.HairGradientDirection;

        var facialHairGradientEnabled = appearance.FacialHairGradientEnabled;
        var facialHairGradientSecondaryColor = ClampColor(appearance.FacialHairGradientSecondaryColor);
        var facialHairGradientDirection = appearance.FacialHairGradientDirection;

        var allMarkingsGradientEnabled = appearance.AllMarkingsGradientEnabled;
        var allMarkingsGradientSecondaryColor = ClampColor(appearance.AllMarkingsGradientSecondaryColor);
        var allMarkingsGradientDirection = appearance.AllMarkingsGradientDirection;

        var width = appearance.Width;
        var height = appearance.Height;

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

            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            skinColor = strategy.EnsureVerified(skinColor);

            width = Math.Clamp(width, speciesProto.MinWidth, speciesProto.MaxWidth); // Sunrise
            height = Math.Clamp(height, speciesProto.MinHeight, speciesProto.MaxHeight); // Sunrise

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
            markingSet.FilterSponsor(sponsorPrototypes, markingManager); // Sunrise-Sponsors
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList(),
            width,
            height,
            hairGradientEnabled,
            hairGradientSecondaryColor,
            hairGradientDirection,
            facialHairGradientEnabled,
            facialHairGradientSecondaryColor,
            facialHairGradientDirection,
            allMarkingsGradientEnabled,
            allMarkingsGradientSecondaryColor,
            allMarkingsGradientDirection);
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
        if (HairGradientEnabled != other.HairGradientEnabled) return false; //Sunrise start
        if (!HairGradientSecondaryColor.Equals(other.HairGradientSecondaryColor)) return false;
        if (HairGradientDirection != other.HairGradientDirection) return false;
        if (FacialHairGradientEnabled != other.FacialHairGradientEnabled) return false;
        if (!FacialHairGradientSecondaryColor.Equals(other.FacialHairGradientSecondaryColor)) return false;
        if (FacialHairGradientDirection != other.FacialHairGradientDirection) return false;
        if (AllMarkingsGradientEnabled != other.AllMarkingsGradientEnabled) return false;
        if (!AllMarkingsGradientSecondaryColor.Equals(other.AllMarkingsGradientSecondaryColor)) return false;
        if (AllMarkingsGradientDirection != other.AllMarkingsGradientDirection) return false; //Sunrise end
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
                HairGradientEnabled == other.HairGradientEnabled && //Sunrise start
                HairGradientSecondaryColor.Equals(other.HairGradientSecondaryColor) &&
                HairGradientDirection == other.HairGradientDirection &&
                FacialHairGradientEnabled == other.FacialHairGradientEnabled &&
                FacialHairGradientSecondaryColor.Equals(other.FacialHairGradientSecondaryColor) &&
                FacialHairGradientDirection == other.FacialHairGradientDirection &&
                AllMarkingsGradientEnabled == other.AllMarkingsGradientEnabled &&
                AllMarkingsGradientSecondaryColor.Equals(other.AllMarkingsGradientSecondaryColor) &&
                AllMarkingsGradientDirection == other.AllMarkingsGradientDirection && //Sunrise end
               Width == other.Width && //starlight
               Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        var h1 = HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings); //Sunrise start
        var h2 = HashCode.Combine(HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection);
        var h3 = HashCode.Combine(FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection);
        var h4 = HashCode.Combine(AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
        var h5 = HashCode.Combine(Width, Height);
        return HashCode.Combine(h1, h2, h3, h4, h5); //Sunrise end
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
