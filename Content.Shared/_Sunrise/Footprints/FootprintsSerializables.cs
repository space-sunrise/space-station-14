﻿using System.Numerics;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Footprints;

/// <summary>
/// Component that represents a single footprint entity in the world
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FootprintComponent : Component
{
    /// <summary>
    /// Entity that created this footprint
    /// </summary>
    [AutoNetworkedField]
    public EntityUid CreatorEntity;

    /// <summary>
    /// Name of the solution container for this footprint
    /// </summary>
    [DataField]
    public string ContainerName = "step";

    /// <summary>
    /// Reference to the solution component containing reagents
    /// </summary>
    [DataField]
    public Entity<SolutionComponent>? SolutionContainer;
}

/// <summary>
/// Component that handles footprint creation when entities step in puddles
/// </summary>
[RegisterComponent]
public sealed partial class PuddleFootprintComponent : Component
{
    /// <summary>
    /// Ratio determining how much of puddle's color transfers to footprints
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferVolume = 2.5f;

    /// <summary>
    /// Percentage of water content above which footprints won't be created
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float WaterThresholdPercent = 80f;
}

/// <summary>
/// Component that manages footprint creation for entities that can leave tracks
/// </summary>
[RegisterComponent]
public sealed partial class FootprintEmitterComponent : Component
{
    /// <summary>
    /// Path to the RSI file containing footprint sprites
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public ResPath SpritePath = new("/Textures/_Sunrise/Effects/footprints.rsi");

    /// <summary>
    /// State ID for left bare footprint
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string LeftBareFootState = "footprint-left-bare-human";

    /// <summary>
    /// State ID for right bare footprint
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string RightBareFootState = "footprint-right-bare-human";

    /// <summary>
    /// State ID for shoe footprint
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string ShoeFootState = "footprint-shoes";

    /// <summary>
    /// State ID for pressure suit footprint
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string PressureSuitFootState = "footprint-suit";

    /// <summary>
    /// Array of state IDs for dragging animations
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] DraggingStates =
    [
        "dragging-1",
        "dragging-2",
        "dragging-3",
        "dragging-4",
        "dragging-5",
    ];

    /// <summary>
    /// Prototype ID for footprint entity
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public EntProtoId<FootprintComponent> FootprintPrototype = "Footstep";

    /// <summary>
    /// Distance between footprints when walking
    /// </summary>
    [DataField]
    public float WalkStepInterval = 0.7f;

    /// <summary>
    /// Distance between marks when being dragged
    /// </summary>
    [DataField]
    public float DragMarkInterval = 0.5f;

    [ViewVariables(VVAccess.ReadOnly), DataField("solution")]
    public string SolutionName = "foots";

    [ViewVariables(VVAccess.ReadOnly), DataField("solutionRef")]
    public Entity<SolutionComponent>? Solution;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferVolume = 0.1f;

    /// <summary>
    /// Rate at which footprint color fades
    /// </summary>
    [DataField]
    public float ColorFadeRate = 0.05f;

    /// <summary>
    /// Offset from entity center for footprint placement
    /// </summary>
    [DataField]
    public Vector2 PlacementOffset = new(0.1f, 0f);

    /// <summary>
    /// Tracks which foot is currently stepping
    /// </summary>
    public bool IsRightStep = true;

    /// <summary>
    /// Position of last footprint
    /// </summary>
    public Vector2 LastStepPosition = Vector2.Zero;
}

/// <summary>
/// Visual states for footprint appearances
/// </summary>
[Serializable, NetSerializable]
public enum FootprintVisualType : byte
{
    BareFootprint,
    ShoeFootprint,
    SuitFootprint,
    DragMark
}

/// <summary>
/// Visual state parameters for footprints
/// </summary>
[Serializable, NetSerializable]
public enum FootprintVisualParameter : byte
{
    VisualState,
    TrackColor
}

/// <summary>
/// Sprite layers for footprint visuals
/// </summary>
[Serializable, NetSerializable]
public enum FootprintSpriteLayer : byte
{
    MainLayer
}
