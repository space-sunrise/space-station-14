using Content.Server._Sunrise.Messenger;
using Content.Shared.CartridgeLoader;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Часть системы картриджа мессенджера, отвечающая за подключение к серверу
/// </summary>
public sealed partial class MessengerCartridgeSystem
{
    private void CheckServerStatus(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid)
    {
        if (!TryGetPdaAndDeviceNetwork(loaderUid, out var pdaUid, out _))
            return;

        var station = _stationSystem.GetOwningStation(pdaUid);
        if (station == null)
        {
            component.ServerAddress = null;
            component.IsRegistered = false;
            component.LastRegistrationAttempt = null;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (!_singletonServer.TryGetActiveServerAddress<MessengerServerComponent>(station.Value, out var serverAddress))
        {
            component.ServerAddress = null;
            component.IsRegistered = false;
            component.LastRegistrationAttempt = null;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (component.ServerAddress != serverAddress)
        {
            component.ServerAddress = serverAddress;
            component.IsRegistered = false;
            component.UserId = null;
            component.LastRegistrationAttempt = null;
            if (TryGetPdaAndDeviceNetwork(loaderUid, out _, out var deviceNetwork))
            {
                TryConnectToServer(uid, component, loaderUid);
            }
            UpdateUiState(uid, loaderUid, component);
        }
        else if (component.ServerAddress == null)
        {
            component.IsRegistered = false;
            component.UserId = null;
            component.LastRegistrationAttempt = null;
            UpdateUiState(uid, loaderUid, component);
        }
        else if (!component.IsRegistered)
        {
            if (TryGetPdaAndDeviceNetwork(loaderUid, out _, out var deviceNetwork))
            {
                TryConnectToServer(uid, component, loaderUid);
            }
            UpdateUiState(uid, loaderUid, component);
        }
        else
        {
            var currentTime = _gameTiming.CurTime;
            if (!component.LastUsersUpdate.HasValue ||
                (currentTime - component.LastUsersUpdate.Value).TotalSeconds >= 10.0)
            {
                component.LastUsersUpdate = currentTime;
                if (TryGetPdaAndDeviceNetwork(loaderUid, out _, out var deviceNetwork))
                {
                    RequestUsers(uid, component, loaderUid, deviceNetwork);
                    RequestGroups(uid, component, loaderUid, deviceNetwork);
                }
            }
            UpdateUiState(uid, loaderUid, component);
        }
    }

    private void OnCartridgeActivated(EntityUid uid, MessengerCartridgeComponent component, CartridgeActivatedEvent args)
    {
        TryConnectToServer(uid, component, args.Loader);
    }

    private void OnCartridgeAdded(EntityUid uid, MessengerCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.LoaderUid = args.Loader;
        TryConnectToServer(uid, component, args.Loader);

        _cartridgeLoader.RegisterBackgroundProgram(args.Loader, uid);
    }

    private void OnUiReady(EntityUid uid, MessengerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        if (!component.IsRegistered && component.ServerAddress == null)
        {
            TryConnectToServer(uid, component, args.Loader);
        }
        else
        {
            UpdateUiState(uid, args.Loader, component);
        }
    }

    private void TryConnectToServer(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid)
    {
        if (!TryGetPdaAndDeviceNetwork(loaderUid, out var pdaUid, out var deviceNetwork))
        {
            Sawmill.Warning($"Failed to get PDA and DeviceNetwork: {ToPrettyString(loaderUid)}");
            return;
        }

        component.LoaderUid = loaderUid;

        var station = _stationSystem.GetOwningStation(pdaUid);
        if (station == null)
        {
            Sawmill.Warning($"No station found for PDA: {ToPrettyString(pdaUid)}");
            component.ServerAddress = null;
            component.IsRegistered = false;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (!_singletonServer.TryGetActiveServerAddress<MessengerServerComponent>(station.Value, out var serverAddress))
        {
            Sawmill.Warning($"No active messenger server found on station: {ToPrettyString(station.Value)}");
            component.ServerAddress = null;
            component.IsRegistered = false;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (string.IsNullOrEmpty(serverAddress))
        {
            Sawmill.Warning($"Server address is empty, server may not be connected to DeviceNetwork yet");
            component.ServerAddress = null;
            component.IsRegistered = false;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        Sawmill.Debug($"Found active server address: {serverAddress}");

        if (component.ServerAddress != serverAddress)
        {
            component.ServerAddress = serverAddress;
            component.IsRegistered = false;
            component.UserId = null;
            component.LastRegistrationAttempt = null;
        }

        if (component.IsRegistered && component.ServerAddress == serverAddress)
        {
            Sawmill.Debug($"Already registered, updating UI state");
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (!component.IsRegistered)
        {
            RegisterUser(uid, component, loaderUid, deviceNetwork, pdaUid);
        }
    }

    private void RegisterUser(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, EntityUid pdaUid)
    {
        if (component.ServerAddress == null)
        {
            Sawmill.Warning($"Cannot register: ServerAddress is null");
            return;
        }

        var currentTime = _gameTiming.CurTime;
        if (component.LastRegistrationAttempt.HasValue)
        {
            var timeSinceLastAttempt = currentTime - component.LastRegistrationAttempt.Value;
            if (timeSinceLastAttempt.TotalSeconds < 5.0)
            {
                Sawmill.Debug($"Registration attempt too soon, waiting: {timeSinceLastAttempt.TotalSeconds:F2}s");
                return;
            }
        }

        component.LastRegistrationAttempt = currentTime;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdRegisterUser,
            [MessengerCommands.CmdRegisterUser] = new NetworkPayload
            {
                ["pda_uid"] = GetNetEntity(pdaUid)
            }
        };

        uint? messengerFrequency = GetMessengerFrequency();
        if (!messengerFrequency.HasValue)
        {
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(deviceNetwork.DeviceNetId, component.ServerAddress))
        {
            Sawmill.Warning($"Server address {component.ServerAddress} is not present in network {deviceNetwork.DeviceNetId}");
            return;
        }

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            Sawmill.Error($"Failed to get DeviceNetworkComponent after setting frequency");
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        Sawmill.Debug($"PDA DeviceNetwork: Address={pdaDevice.Address}, TransmitFrequency={pdaDevice.TransmitFrequency}, ReceiveFrequency={pdaDevice.ReceiveFrequency}, DeviceNetId={pdaDevice.DeviceNetId}");

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            Sawmill.Warning($"Server address {component.ServerAddress} is not present in network {pdaDevice.DeviceNetId} (PDA network)");
        }
        else
        {
            Sawmill.Debug($"Server address {component.ServerAddress} found in network {pdaDevice.DeviceNetId}");
        }

        var pdaTransform = Transform(loaderUid);
        var pdaPos = _transformSystem.GetWorldPosition(pdaTransform);
        Sawmill.Debug($"PDA position: {pdaPos}, MapId: {pdaTransform.MapID}");

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFrequency, network: pdaDevice.DeviceNetId);

        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }
}
