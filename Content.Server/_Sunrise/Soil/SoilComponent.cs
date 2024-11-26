using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Soil;

/// <summary>
/// Компонент для мешка с землей.
/// </summary>
[RegisterComponent]
public sealed partial class SoilComponent : Component
{
    [DataField(readOnly: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPrototype = "hydroponicsSoil";

    [DataField]
    public string PopupStringFailed = "soil-plant-failed";

    [DataField]
    public string PopupStringSuccess = "soil-plant-success";

    [DataField]
    public float StaminaDamage = 30f;
}
