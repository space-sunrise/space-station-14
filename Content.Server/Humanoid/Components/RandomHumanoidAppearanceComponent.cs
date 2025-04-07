using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Content.Shared.Humanoid.Markings; // Sunrise-Edit

namespace Content.Server.CharacterAppearance.Components;

[RegisterComponent]
public sealed partial class RandomHumanoidAppearanceComponent : Component
{
    [DataField("randomizeName")] public bool RandomizeName = true;

    /// <summary>
    /// After randomizing, sets the hair style to this, if possible
    /// </summary>
    [DataField] public string? Hair = null;

    // Sunrise-Start
    /// <summary>
    /// Настраивает цвет кожи в HEX формате. Учитывайте что только бежевые и темные цвета могут подойти человекоподобным. Советую подбирать цвет прям в игре.
    /// </summary>
    [DataField] 
    public Color? SkinColor = null;

    /// <summary>
    /// Настраивает цвет волос в HEX формате
    /// </summary>
    [DataField]
    public Color? HairColor = null;

    /// <summary>
    /// Настраивает цвет бороды в HEX формате
    /// </summary>
    [DataField]
    public Color? FacialHairColor = null;
    // Sunrise-End
}