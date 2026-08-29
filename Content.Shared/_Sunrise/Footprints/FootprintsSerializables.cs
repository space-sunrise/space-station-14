using System;
using System.Numerics;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Footprints;

/// <summary>
/// Компонент, представляющий отдельный след в мире.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FootprintComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public ResPath SpritePath = new("/Textures/_Sunrise/Effects/footprints.rsi");

    /// <summary>
    /// Имя контейнера раствора для этого следа.
    /// </summary>
    [DataField]
    public string ContainerName = "step";

    /// <summary>
    /// Ссылка на компонент раствора с реагентами.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? SolutionContainer;

    [DataField]
    public PrintType PrintType;
}

public enum PrintType
{
    DragMark,
    Foot
}

/// <summary>
/// Компонент, отвечающий за создание следов, когда сущности наступают в лужи.
/// </summary>
[RegisterComponent]
public sealed partial class PuddleFootprintComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferVolume = 15f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float WaterThresholdPercent = 75f;
}

/// <summary>
/// Компонент, управляющий созданием следов у сущностей, которые могут их оставлять.
/// </summary>
[RegisterComponent]
public sealed partial class FootprintEmitterComponent : Component
{
    /// <summary>
    /// ID состояния для левого босого следа.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] LeftBareFootState =
    {
        "footprint-left-bare-human",
    };

    /// <summary>
    /// ID состояния для правого босого следа.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] RightBareFootState =
    {
        "footprint-right-bare-human",
    };

    /// <summary>
    /// ID состояния для следа обуви.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] ShoeFootState =
    {
        "footprint-shoes",
    };

    /// <summary>
    /// ID состояния для следа скафандра.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] PressureSuitFootState =
    {
        "footprint-suit",
    };

    /// <summary>
    /// Массив ID состояний для анимаций волочения.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] DraggingStates =
    {
        "dragging-1",
        "dragging-2",
        "dragging-3",
        "dragging-4",
        "dragging-5",
    };

    /// <summary>
    /// ID прототипа сущности следа.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public EntProtoId<FootprintComponent> FootprintPrototype = "Footstep";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public EntProtoId<FootprintComponent> DragMarkPrototype = "DragMark";

    /// <summary>
    /// Расстояние между следами при ходьбе.
    /// </summary>
    [DataField]
    public float WalkStepInterval = 0.7f;

    /// <summary>
    /// Расстояние между отметками при волочении.
    /// </summary>
    [DataField]
    public float DragMarkInterval = 0.5f;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string FootsSolutionName = "foots";

    [ViewVariables]
    public Entity<SolutionComponent>? FootsSolution;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string BodySurfaceSolutionName = "body_surface";

    [ViewVariables]
    public Entity<SolutionComponent>? BodySurfaceSolution;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferVolumeFoot = 2.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferVolumeDragMark = 5.0f;

    /// <summary>
    /// Смещение от центра сущности для размещения следа.
    /// </summary>
    [DataField]
    public Vector2 PlacementOffset = new(0.1f, 0f);

    /// <summary>
    /// Отслеживает, какая нога делает текущий шаг.
    /// </summary>
    public bool IsRightStep = true;

    /// <summary>
    /// Позиция последнего следа.
    /// </summary>
    public Vector2 LastStepPosition = Vector2.Zero;

    /// <summary>
    /// Timestamp (game time) until which the emitter cannot absorb fluid from puddles.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PuddleAbsorptionCooldownUntil = TimeSpan.Zero;
}

/// <summary>
/// Визуальные состояния внешнего вида следов.
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
/// Параметры визуального состояния следов.
/// </summary>
[Serializable, NetSerializable]
public enum FootprintVisualParameter : byte
{
    VisualState,
    TrackColor
}

/// <summary>
/// Слои спрайта для визуала следов.
/// </summary>
[Serializable, NetSerializable]
public enum FootprintSpriteLayer : byte
{
    MainLayer
}
