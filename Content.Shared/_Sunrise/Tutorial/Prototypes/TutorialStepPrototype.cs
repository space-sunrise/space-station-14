using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

/// <summary>
///     Prototype describing a single step in a tutorial sequence.
///     A step may display a tutorial bubble, send chat and/or TTS messages,
///     and is completed when all specified conditions are satisfied.
/// </summary>
[Prototype]
public sealed partial class TutorialStepPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<TutorialStepPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     The primary entity prototype this step is about.
    ///     Used for navigation (path overlay) and as the bubble anchor when
    ///     <see cref="TutorialBubbleData.AttachToTarget"/> is set.
    /// </summary>
    [DataField]
    public EntProtoId? Target;

    /// <summary>
    ///     Optional tutorial bubble displayed to the player during this step.
    /// </summary>
    [DataField]
    public TutorialBubbleData? Bubble;

    /// <summary>
    ///     Chat message sent when this step becomes active.
    /// </summary>
    [DataField]
    public string ChatMessage = string.Empty;

    /// <summary>
    ///     Text passed to the TTS system when this step becomes active.
    /// </summary>
    [DataField]
    public string TtsMessage = string.Empty;

    /// <summary>
    ///     Sender name used for chat message (like narrator).
    /// </summary>
    [DataField]
    public string Sender = "base-tutorial-sender";

    /// <summary>
    ///     TTS voice prototype used to play the TTS message.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> VoiceId = "Jirinovskiy"!;

    /// <summary>
    ///     If true, this step may be skipped without blocking tutorial progress.
    /// </summary>
    [DataField]
    public bool Optional;

    /// <summary>
    ///     Range that used for finding entities in conditions
    /// </summary>
    [DataField]
    public int ObserveRange = 10;

    /// <summary>
    ///     Conditions that must be satisfied to complete this tutorial step.
    /// </summary>
    [DataField]
    public List<TutorialCondition> Conditions = [];

    /// <summary>
    ///     Alternative conditions. If set, any of these must be satisfied in addition to <see cref="Conditions"/>.
    /// </summary>
    [DataField]
    public List<TutorialCondition> AnyConditions = [];

    /// <summary>
    ///     Conditions that must be met before this step is considered active.
    /// </summary>
    [DataField]
    public List<TutorialCondition> Preconditions = [];

    /// <summary>
    ///     Optional step to jump to if <see cref="Preconditions"/> are not met.
    ///     If unset, failed preconditions skip this step and continue with the next one.
    /// </summary>
    [DataField]
    public ProtoId<TutorialStepPrototype>? PreconditionFailStep;
}

/// <summary>
///     Data describing a tutorial bubble shown to the player.
/// </summary>
[DataDefinition]
public sealed partial class TutorialBubbleData
{
    /// <summary>
    ///     Text displayed inside the tutorial bubble.
    /// </summary>
    [DataField(required: true)]
    public string Text = string.Empty;

    /// <summary>
    ///     If <c>true</c>, the bubble is anchored to the entity matched by
    ///     <see cref="TutorialStepPrototype.Target"/>. Otherwise it is anchored
    ///     to the player.
    /// </summary>
    [DataField]
    public bool AttachToTarget;
}
