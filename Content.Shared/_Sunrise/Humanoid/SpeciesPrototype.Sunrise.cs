using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype
{
    /// <summary>
    /// Whether the species is only available to sponsors.
    /// </summary>
    [DataField]
    public bool SponsorOnly { get; private set; }

    /// <summary>
    /// Body type prototypes available to this species.
    /// </summary>
    [DataField(required: true)]
    public List<string> BodyTypes { get; private set; } = default!;

    /// <summary>
    /// Dataset used for masculine last names.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> MaleLastNames { get; private set; } = "NamesLast";

    /// <summary>
    /// Dataset used for feminine last names.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> FemaleLastNames { get; private set; } = "NamesLast";

    /// <summary>
    /// Minimum character width multiplier.
    /// </summary>
    [DataField]
    public float MinWidth = 0.95f;

    /// <summary>
    /// Maximum character width multiplier.
    /// </summary>
    [DataField]
    public float MaxWidth = 1.1f;

    /// <summary>
    /// Default character width multiplier.
    /// </summary>
    [DataField]
    public float DefaultWidth = 1f;

    /// <summary>
    /// Minimum character height multiplier.
    /// </summary>
    [DataField]
    public float MinHeight = 0.9f;

    /// <summary>
    /// Maximum character height multiplier.
    /// </summary>
    [DataField]
    public float MaxHeight = 1.1f;

    /// <summary>
    /// Default character height multiplier.
    /// </summary>
    [DataField]
    public float DefaultHeight = 1f;

    /// <summary>
    /// Minimum displayed height in centimeters.
    /// </summary>
    [DataField]
    public float MinHeightCm = 150f;

    /// <summary>
    /// Maximum displayed height in centimeters.
    /// </summary>
    [DataField]
    public float MaxHeightCm = 200f;

    /// <summary>
    /// Weight in kilograms at the default height and width.
    /// </summary>
    [DataField]
    public int StandardWeight = 75;

    /// <summary>
    /// Density used to scale weight with character size.
    /// </summary>
    [DataField]
    public int StandardDensity = 120;

    /// <summary>
    /// Whether station records hide this species by default.
    /// </summary>
    [DataField]
    public bool StationRecordsHidden;

    /// <summary>
    /// Sprite used for species previews.
    /// </summary>
    [DataField]
    public SpriteSpecifier Preview { get; private set; } =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Species/Human/parts.rsi"), "full");

    /// <summary>
    /// Sprite used by the Sunrise butt scanner.
    /// </summary>
    [DataField]
    public SpriteSpecifier ButtScan =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Sunrise/CopyMachine/butts_scans.rsi"), "human");
}
