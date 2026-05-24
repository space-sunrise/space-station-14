using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

/// <summary>
///     Prototype describing a tutorial sequence.
///     Defines the data required to start and display a tutorial,
///     including its UI representation, player entity, grid, and ordered steps.
/// </summary>
[Prototype]
public sealed partial class TutorialSequencePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Localized name displayed in the tutorial selection UI.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    ///     Tooltip shown when hovering over the tutorial entry in the UI.
    /// </summary>
    [DataField]
    public string Tooltip = string.Empty;

    /// <summary>
    ///     Grid map loaded for this tutorial sequence.
    /// </summary>
    [DataField(required: true)]
    public ResPath Grid;

    /// <summary>
    ///     Player entity prototype used when starting this tutorial.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId PlayerEntity;

    /// <summary>
    ///     Texture displayed for this tutorial in the tutorial menu.
    /// </summary>
    [DataField(required: true)]
    public ResPath Texture;

    /// <summary>
    ///     Duration of tutorial
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Duration;

    /// <summary>
    ///     Ordered list of tutorial steps that make up this sequence.
    /// </summary>
    [DataField]
    public List<ProtoId<TutorialStepPrototype>> Steps = [];
}
