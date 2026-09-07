using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Собственный, полностью независимый от ванильного OreSilo, механизм хранилища материалов на весь грид.
/// Раздаёт материалы всем подключённым <see cref="SunriseMaterialSiloClientComponent"/> в пределах того же грида.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSunriseMaterialSiloSystem))]
public sealed partial class SunriseMaterialSiloComponent : Component
{
    /// <summary>
    /// Подключённые к этому силосу <see cref="SunriseMaterialSiloClientComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Clients = new();
}

[Serializable, NetSerializable]
public sealed class SunriseMaterialSiloBuiState : BoundUserInterfaceState
{
    public readonly HashSet<(NetEntity, string, string)> Clients;

    public SunriseMaterialSiloBuiState(HashSet<(NetEntity, string, string)> clients)
    {
        Clients = clients;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleSunriseMaterialSiloClientMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Client;

    public ToggleSunriseMaterialSiloClientMessage(NetEntity client)
    {
        Client = client;
    }
}

[Serializable, NetSerializable]
public enum SunriseMaterialSiloUiKey : byte
{
    Key
}
