using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Сущность с <see cref="Content.Shared.Materials.MaterialStorageComponent"/>, подключённая к <see cref="SunriseMaterialSiloComponent"/>.
/// Полностью независима от ванильного <c>OreSiloClientComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSunriseMaterialSiloSystem))]
public sealed partial class SunriseMaterialSiloClientComponent : Component
{
    /// <summary>
    /// Силос, из которого эта сущность получает материалы.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Silo;
}
