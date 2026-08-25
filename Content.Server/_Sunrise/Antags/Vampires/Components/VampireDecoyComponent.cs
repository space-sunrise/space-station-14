using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antags.Vampires.Components;

/// <summary>
/// Обрабатывает простое поведение сущностей-приманок вампира.
/// </summary>
[RegisterComponent]
public sealed partial class VampireDecoyComponent : Component
{
    /// <summary>
    /// Гарантирует, что вспышка/взрыв произойдёт только один раз.
    /// </summary>
    public bool Detonated;

    [DataField]
    public EntProtoId FlashEffectId = "GrenadeFlashEffect";

    [DataField]
    public float FlashRange = 3f;

    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public float SlowTo = 0.5f;

    [DataField]
    public bool DisplayPopup = true;

    [DataField]
    public float Probability = 1f;

    [DataField]
    public SoundSpecifier FlashSound = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");
}
