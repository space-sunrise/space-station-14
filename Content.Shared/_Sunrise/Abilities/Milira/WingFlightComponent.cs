using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.Abilities.Milira;

/// <summary>
/// Компонент, добавляющий действие полёта и хранящий общее состояние способности.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState(true)]
public sealed partial class WingFlightComponent : Component
{
    /// <summary>
    /// Прототип действия, переключающего состояние полёта.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionToggleFlight";

    /// <summary>
    /// Суффикс маркинга, используемый при активном полёте.
    /// </summary>
    [DataField]
    public string Suffix = "Flight";

    /// <summary>
    /// Экземпляр действия полёта на сущности.
    /// </summary>
    public EntityUid? ActionEntity;

    /// <summary>
    /// Множитель скорости ходьбы и бега во время полёта.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.3f;

    /// <summary>
    /// Модификатор наземного трения при полёте и во время инерции.
    /// Значения ниже 1 уменьшают трение и удлиняют скольжение.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionModifier = 0.08f;

    /// <summary>
    /// Длительность инерции после выключения полёта.
    /// </summary>
    [DataField]
    public TimeSpan InertiaDuration = TimeSpan.FromSeconds(2.0);

    /// <summary>
    /// Активен ли полёт сейчас.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FlightEnabled;

    /// <summary>
    /// Активна ли пост-флайтовая инерция.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InertiaActive;

    /// <summary>
    /// Максимальный множитель масштаба спрайта во время полёта.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxScaleMultiplier = 1.35f;

    /// <summary>
    /// Минимальный множитель масштаба, когда визуал полёта отсутствует.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinScaleMultiplier = 1.0f;

    /// <summary>
    /// Текущий множитель масштаба, синхронизируемый с клиентами.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentScaleMultiplier = 1.0f;

    /// <summary>
    /// Скорость приближения масштаба к целевому значению.
    /// </summary>
    [DataField]
    public float ScaleLerpRate = 4f;

    /// <summary>
    /// Минимальная доля выносливости (0..1) для активации.
    /// </summary>
    [DataField]
    public float ActivationThreshold = 0.5f;

    /// <summary>
    /// Разовый расход выносливости при запуске полёта.
    /// </summary>
    [DataField]
    public float ActivationStaminaDamage = 20f;

    /// <summary>
    /// Постоянный расход выносливости в секунду при активном полёте.
    /// </summary>
    [DataField]
    public float SustainStaminaPerSecond = 5f;

    /// <summary>
    /// Доля выносливости (0..1), при которой полёт выключается принудительно.
    /// </summary>
    [DataField]
    public float AutoDisableThreshold = 0.25f;

    /// <summary>
    /// Момент завершения инерции (только сервер).
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? InertiaEndTime;

    /// <summary>
    /// Аккумулятор для периодического расхода выносливости (только сервер).
    /// </summary>
    [ViewVariables]
    public float SustainAccumulator;

    /// <summary>
    /// Применялся ли суффикс полёта, чтобы при выключении вернуть маркинг обратно.
    /// </summary>
    [ViewVariables]
    public bool AppliedMarkingOnEnable;

    /// <summary>
    /// Список оригинальных маркингов для восстановления после отключения полёта (только сервер).
    /// </summary>
    [ViewVariables]
    public Dictionary<int, string> OriginalMarkings = new();

    /// <summary>
    /// Исходные коллизии фикстур, чтобы при завершении полёта вернуть проход блокирующих тайлов.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, int> OriginalCollisionMasks = new();

    /// <summary>
    /// Исходные слои фикстур, сохраняются вместе с масками для симметричного возврата.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, int> OriginalCollisionLayers = new();

    /// <summary>
    /// Оригинальный масштаб спрайта (только клиент, для визуализации).
    /// </summary>
    [ViewVariables]
    public Vector2? OriginalScale;
}

/// <summary>
/// Маркерный компонент для сущностей с активным полетом, требующих обновления в Update.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveWingFlightComponent : Component
{
}

