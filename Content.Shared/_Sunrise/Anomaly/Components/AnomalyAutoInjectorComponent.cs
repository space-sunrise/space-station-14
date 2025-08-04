using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Components;

[RegisterComponent]
public sealed partial class AnomalyAutoInjectorComponent : Component // ниже значения по умолчанию, если не указали в yml
{
    [DataField("cellularDamage")] public int CellularDamage = 50; // урон лечится фалангимином и доксарубиксадоном
    [DataField("anomalyDelay")] public int AnomalyDelay = 60; // через сколько сек цель первратится в аномалию после иньекции
    [DataField("rainbowDuration")] public int RainbowDuration = 60; // появляется от 20сек!
    [DataField("rainbowEffect")] public string RainbowEffect = "SeeingRainbows"; // слабый эффект галлюцинаций
    [DataField("popupNothingToInject")] public string PopupNothingToInject = "Нечего вводить!";
    [DataField("popupNotApplicable")] public string PopupNotApplicable = "Неприменимо!"; // остальные мобы, кроме гуманодов
    [DataField("popupPending")] public string PopupPending = "Кожа не поддаётся инъекции!"; // стадия заражения после инъекции
    [DataField("popupInfected")] public string PopupInfected = "Кожа не поддаётся инъекции!"; // уже когда превратился в аномалию
    [DataField("hypospraySound")] public string HypospraySound = "/Audio/Items/hypospray.ogg";
    [DataField("anomalyTrapProtos")] public List<string> AnomalyTrapProtos = new()
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
