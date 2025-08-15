// ключи локализации не используются, т.к. данная разработка только на одном языке, включая комментарии для кода.
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Anomaly.Components;

[RegisterComponent]
public sealed partial class AnomalyAutoInjectorComponent : Component // ниже значения по умолчанию, если не указали в yml
{
    [DataField("cellularDamage"), ViewVariables] public int CellularDamage = 50; // урон лечится фалангимином и доксарубиксадоном
    [DataField("anomalyDelay"), ViewVariables] public int AnomalyDelay = 60; // через сколько сек цель первратится в аномалию после иньекции
    [DataField("rainbowDuration"), ViewVariables] public int RainbowDuration = 60; // появляется от 20сек!
    [DataField("rainbowEffect"), ViewVariables] public EntProtoId RainbowEffect = "StatusEffectSeeingRainbows"; // слабый эффект галлюцинаций
    [DataField("rainbowEffectIntensity"), ViewVariables] public float RainbowEffectIntensity = 0.1f; // интенсивность эффекта галлюцинаций
    [DataField("popupNothingToInject"), ViewVariables] public string PopupNothingToInject = "Нечего вводить!";
    [DataField("popupNotApplicable"), ViewVariables] public string PopupNotApplicable = "Неприменимо!"; // остальные мобы, кроме гуманодов
    [DataField("popupPending"), ViewVariables] public string PopupPending = "Кожа не поддаётся инъекции!"; // стадия заражения после инъекции
    [DataField("popupInfected"), ViewVariables] public string PopupInfected = "Кожа не поддаётся инъекции!"; // уже когда превратился в аномалию
    [DataField("hypospraySound"), ViewVariables] public string HypospraySound = "/Audio/Items/hypospray.ogg";
    [DataField("anomalyTrapProtos"), ViewVariables] public List<EntProtoId> AnomalyTrapProtos = new();
}

[RegisterComponent, NetworkedComponent] // Используется для смены спрайта и блокировки повторного использования инъектора
public sealed partial class UsedAnomalyAutoInjectorComponent : Component
{
	[DataField("spriteStateFull"), ViewVariables] public string SpriteStateFull = "anomagen";
	[DataField("spriteStateEmpty"), ViewVariables] public string SpriteStateEmpty = "anomagen_empty";
}
