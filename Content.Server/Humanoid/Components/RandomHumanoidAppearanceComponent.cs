using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

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
    /// Настраивает цвет кожи в HEX формате, учитывайте то что большинство цветов не подойдёт из-за искуственных ограничений в цветах кожи.
    /// </summary>
    [DataField] public Color? SkinColor = null; 
    // Sunrise-End
}
