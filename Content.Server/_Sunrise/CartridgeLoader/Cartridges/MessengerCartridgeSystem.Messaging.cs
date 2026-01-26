using Content.Server._Sunrise.Messenger;
using Content.Shared.CartridgeLoader;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Часть системы картриджа мессенджера, отвечающая за отправку сообщений и запросы
/// </summary>
public sealed partial class MessengerCartridgeSystem
{
    private void OnUiMessage(EntityUid uid, MessengerCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not MessengerUiMessageEvent message)
            return;

        var loaderUid = GetEntity(args.LoaderUid);
        if (!TryGetPdaAndDeviceNetwork(loaderUid, out var pdaUid, out var deviceNetwork))
            return;

        switch (message.Action)
        {
            case MessengerUiAction.SendMessage:
                if (message.Content != null)
                    SendMessage(uid, component, loaderUid, deviceNetwork, message.RecipientId, message.GroupId, message.Content);
                break;
            case MessengerUiAction.CreateGroup:
                if (message.GroupName != null)
                    CreateGroup(uid, component, loaderUid, deviceNetwork, message.GroupName);
                break;
            case MessengerUiAction.AddToGroup:
                if (message.GroupId != null && message.UserId != null)
                    AddToGroup(uid, component, loaderUid, deviceNetwork, message.GroupId, message.UserId);
                break;
            case MessengerUiAction.RemoveFromGroup:
                if (message.GroupId != null && message.UserId != null)
                    RemoveFromGroup(uid, component, loaderUid, deviceNetwork, message.GroupId, message.UserId);
                break;
            case MessengerUiAction.RequestUsers:
                RequestUsers(uid, component, loaderUid, deviceNetwork);
                break;
            case MessengerUiAction.RequestGroups:
                RequestGroups(uid, component, loaderUid, deviceNetwork);
                break;
            case MessengerUiAction.RequestMessages:
                if (message.ChatId != null)
                    RequestMessages(uid, component, loaderUid, deviceNetwork, message.ChatId);
                break;
            case MessengerUiAction.ToggleMute:
                if (message.ChatId != null && message.IsMuted.HasValue)
                    ToggleMute(uid, component, message.ChatId, message.IsMuted.Value);
                break;
        }
    }

    private void SendMessage(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string? recipientId, string? groupId, string content)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdSendMessage,
            [MessengerCommands.CmdSendMessage] = new NetworkPayload
            {
                ["content"] = content,
                ["recipient_id"] = recipientId ?? string.Empty,
                ["group_id"] = groupId ?? string.Empty
            }
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);

        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void CreateGroup(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string groupName)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdCreateGroup,
            [MessengerCommands.CmdCreateGroup] = new NetworkPayload
            {
                ["name"] = groupName
            }
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void AddToGroup(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string groupId, string userId)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdAddToGroup,
            [MessengerCommands.CmdAddToGroup] = new NetworkPayload
            {
                ["group_id"] = groupId,
                ["user_id"] = userId
            }
        };

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RemoveFromGroup(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string groupId, string userId)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdRemoveFromGroup,
            [MessengerCommands.CmdRemoveFromGroup] = new NetworkPayload
            {
                ["group_id"] = groupId,
                ["user_id"] = userId
            }
        };

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RequestUsers(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork)
    {
        if (component.ServerAddress == null)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdGetUsers
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RequestGroups(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork)
    {
        if (component.ServerAddress == null)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdGetGroups
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RequestMessages(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string chatId)
    {
        if (component.ServerAddress == null)
            return;

        component.LastRequestedChatId = chatId;

        component.ServerUnreadCounts.Remove(chatId);

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdGetMessages,
            [MessengerCommands.CmdGetMessages] = new NetworkPayload
            {
                ["chat_id"] = chatId
            }
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }
}
