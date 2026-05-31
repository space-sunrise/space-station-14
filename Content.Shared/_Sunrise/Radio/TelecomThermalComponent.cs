using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.Radio;

/// <summary>
/// Component for handling telecom server thermals and bandwidth loads in Sunrise.
/// </summary>
[RegisterComponent]
public sealed partial class TelecomThermalComponent : Component
{
    [DataField]
    public int MaxBandwidth = 50;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentLoad = 0f;

    [DataField]
    public float LoadDecayRate = 1f;

    [DataField]
    public float HeatPerMessage = 5000f;

    [DataField]
    public float MaxTemperature = 390f;

    [DataField]
    public float HysteresisTemperature = 360f;

    [ViewVariables]
    public bool Overheated = false;

    [ViewVariables]
    public float AlarmTimer = 0f;

    [DataField]
    public float AlarmInterval = 3f;

    [DataField]
    public SoundSpecifier? OverheatSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/beeps.ogg");

    // Extracted magic numbers:

    /// <summary>
    /// Divisor for the message load heat generation calculation.
    /// </summary>
    [DataField]
    public float LoadDivisor = 10f;

    /// <summary>
    /// The base ambient temperature used for computing audio and heating ratios.
    /// </summary>
    [DataField]
    public float BaseAmbientTemperature = 300f;

    /// <summary>
    /// The maximum ratio clamp value for ambient calculations.
    /// </summary>
    [DataField]
    public float MaxAmbientRatio = 1.2f;

    /// <summary>
    /// Base volume level for ambient noise when operating.
    /// </summary>
    [DataField]
    public float BaseAmbientVolume = -9f;

    /// <summary>
    /// Volume increase multiplier based on temperature ratio.
    /// </summary>
    [DataField]
    public float AmbientVolumeMultiplier = 12f;

    /// <summary>
    /// Base range/distance for ambient noise.
    /// </summary>
    [DataField]
    public float BaseAmbientRange = 5f;

    /// <summary>
    /// Range increase multiplier based on temperature ratio.
    /// </summary>
    [DataField]
    public float AmbientRangeMultiplier = 15f;

    /// <summary>
    /// Load increment added to the server per processed message.
    /// </summary>
    [DataField]
    public float LoadIncreasePerMessage = 1f;

    /// <summary>
    /// Base temperature threshold above which temperature affects static noise levels.
    /// </summary>
    [DataField]
    public float StaticBaseTemperature = 310f;

    /// <summary>
    /// The radio static factor threshold below which no static/noise is applied.
    /// </summary>
    [DataField]
    public float StaticFactorThreshold = 0.3f;

    /// <summary>
    /// Chance multiplier when calculating static probability.
    /// </summary>
    [DataField]
    public float StaticChanceMultiplier = 0.8f;

    /// <summary>
    /// Chance scaling factor for static applied to whole words.
    /// </summary>
    [DataField]
    public float StaticChanceWordFactor = 0.4f;

    /// <summary>
    /// Probability of character replacement with a period when garbling text.
    /// </summary>
    [DataField]
    public float StaticChancePeriodFactor = 0.4f;
}
