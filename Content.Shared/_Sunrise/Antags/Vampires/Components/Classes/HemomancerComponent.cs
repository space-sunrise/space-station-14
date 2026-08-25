using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HemomancerComponent : VampireClassComponent
{
    /// <summary>
    /// Находится ли вампир сейчас в форме Кровавой лужи.
    /// </summary>
    [AutoNetworkedField]
    public bool InSanguinePool = false;
    /// <summary>
    /// Активен ли Обряд Кровеносца.
    /// </summary>
    [AutoNetworkedField]
    public bool BloodBringersRiteActive = false;
    /// <summary>
    /// Активны ли Кровавые когти.
    /// </summary>
    [AutoNetworkedField]
    public bool HemomancerClawsActive = false;
    /// <summary>
    /// Идентификатор цикла Обряда Кровеносца против дублирующих циклов.
    /// </summary>
    public int BloodBringersRiteLoopId = 0;
}
