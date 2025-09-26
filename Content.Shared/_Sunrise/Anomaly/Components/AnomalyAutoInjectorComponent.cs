// ключи локализации не используются, т.к. данная разработка только на одном языке, включая комментарии для кода.
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Anomaly.Components;

[RegisterComponent]
public sealed partial class AnomalyAutoInjectorComponent : Component // ниже значения по умолчанию, если не указали в yml
{
    [DataField]
    [ViewVariables]
    public int CellularDamage = 50; // урон лечится фалангимином и доксарубиксадоном

    [DataField]
    [ViewVariables]
    public int AnomalyDelay = 60; // через сколько сек цель первратится в аномалию после иньекции

    [DataField]
    [ViewVariables]
    public int RainbowDuration = 60; // появляется от 20сек!

    [DataField]
    [ViewVariables]
    public EntProtoId RainbowEffect = "StatusEffectSeeingRainbows"; // слабый эффект галлюцинаций

    [DataField]
    [ViewVariables]
    public float RainbowEffectIntensity = 0.1f; // интенсивность эффекта галлюцинаций

    // Для локализации: ключи, а не строки. Использовать Loc.GetString(Popup...) при показе.
    [DataField]
    [ViewVariables]
    public string PopupNothingToInject = "uplink-anomaly-auto-injector-popup-nothing-to-inject";

    [DataField]
    [ViewVariables]
    public string PopupNotApplicable = "uplink-anomaly-auto-injector-popup-not-applicable"; // остальные мобы, кроме гуманодов

    [DataField]
    [ViewVariables]
    public string PopupPending = "uplink-anomaly-auto-injector-popup-pending"; // стадия заражения после инъекции

    [DataField]
    [ViewVariables]
    public string PopupInfected = "uplink-anomaly-auto-injector-popup-infected"; // уже когда превратился в аномалию

    [DataField]
    [ViewVariables]
    public string HypospraySound = "/Audio/Items/hypospray.ogg";

    [DataField]
    [ViewVariables]
    public List<EntProtoId> AnomalyTrapProtos = new();
}

public enum AnomalyAutoInjectorVisualLayers : byte
{
    Base
}

[RegisterComponent, NetworkedComponent] // Используется для смены спрайта и блокировки повторного использования инъектора
public sealed partial class UsedAnomalyAutoInjectorComponent : Component
{
    [DataField]
    [ViewVariables]
    public string SpriteStateFull = "anomagen";
    [DataField]
    [ViewVariables]
    public string SpriteStateEmpty = "anomagen_empty";
    [DataField]
    [ViewVariables]
    public AnomalyAutoInjectorVisualLayers SpriteLayer = AnomalyAutoInjectorVisualLayers.Base;
}
