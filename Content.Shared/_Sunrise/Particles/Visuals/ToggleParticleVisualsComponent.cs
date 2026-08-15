using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Configures a persistent particle orchestra that follows an activated toggleable item.
/// </summary>
[RegisterComponent]
public sealed partial class ToggleParticleVisualsComponent : Component
{
    /// <summary>
    /// Orchestra started while the item is activated.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ParticleOrchestraPrototype> Orchestra;

    /// <summary>
    /// Слой предмета, по непрозрачным пикселям которого определяется точка эмиссии на земле.
    /// </summary>
    [DataField]
    public string? SpriteLayer;

    /// <summary>
    /// Запасное смещение, если подходящий визуальный слой отсутствует.
    /// </summary>
    [DataField]
    public Vector2 FallbackOffset = new(0f, 0.25f);
}
