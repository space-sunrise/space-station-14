// ключи локализации не используются, т.к. данная разработка только на одном языке, включая комментарии для кода.
using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Components;

[RegisterComponent]
public sealed partial class AnomalyAutoInjectorComponent : Component // ниже значения по умолчанию, если не указали в yml
{
    [DataField("cellularDamage"), ViewVariables] public int CellularDamage = 50; // урон лечится фалангимином и доксарубиксадоном
    [DataField("anomalyDelay"), ViewVariables] public int AnomalyDelay = 60; // через сколько сек цель первратится в аномалию после иньекции
    [DataField("rainbowDuration"), ViewVariables] public int RainbowDuration = 60; // появляется от 20сек!
    [DataField("rainbowEffect"), ViewVariables] public string RainbowEffect = "SeeingRainbows"; // слабый эффект галлюцинаций
    [DataField("popupNothingToInject"), ViewVariables] public string PopupNothingToInject = "Нечего вводить!";
    [DataField("popupNotApplicable"), ViewVariables] public string PopupNotApplicable = "Неприменимо!"; // остальные мобы, кроме гуманодов
    [DataField("popupPending"), ViewVariables] public string PopupPending = "Кожа не поддаётся инъекции!"; // стадия заражения после инъекции
    [DataField("popupInfected"), ViewVariables] public string PopupInfected = "Кожа не поддаётся инъекции!"; // уже когда превратился в аномалию
    [DataField("hypospraySound"), ViewVariables] public string HypospraySound = "/Audio/Items/hypospray.ogg";
    [DataField("anomalyTrapProtos"), ViewVariables] public List<string> AnomalyTrapProtos = new()
    {
        // без имбовых (Pyroclastic,Electricity,Ice,Shadow,Santa) + (Gravity) - грузит сервак и бесполезен для зека
        "AnomalyTrapFlora",
        "AnomalyTrapFlesh",
        "AnomalyTrapTech",
        "AnomalyTrapRock",
        "AnomalyTrapBluespace",
    };
}

[RegisterComponent, NetworkedComponent] // Используется для смены спрайта и блокировки повторного использования инъектора
public sealed partial class UsedAnomalyAutoInjectorComponent : Component
{
}
