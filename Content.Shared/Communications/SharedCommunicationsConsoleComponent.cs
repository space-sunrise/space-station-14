using Robust.Shared.Serialization;

namespace Content.Shared.Communications
{
    [Virtual]
    public partial class SharedCommunicationsConsoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce;
        public readonly bool CanBroadcast = true;
        public readonly bool CanCall;
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly bool CountdownStarted;
        public List<string>? AlertLevels;
        public string CurrentAlert;
        public float CurrentAlertDelay;
        public readonly List<CommunicationsConsoleAdditionalAlertLevelState> AdditionalAlertLevels; // Sunrise-Edit
        // Sunrise-Start
        public readonly bool CanRelay;
        public readonly bool IsRelaying;
        public readonly float RelayCooldownRemaining;
        public readonly float RelayTimeRemaining;
        // Sunrise-End

        public CommunicationsConsoleInterfaceState(bool canAnnounce, bool canCall, List<string>? alertLevels, string currentAlert, float currentAlertDelay, TimeSpan? expectedCountdownEnd = null, bool canRelay = false, bool isRelaying = false, float relayCooldownRemaining = 0f, float relayTimeRemaining = 0f, List<CommunicationsConsoleAdditionalAlertLevelState>? additionalAlertLevels = null) // Sunrise-Edit
        {
            CanAnnounce = canAnnounce;
            CanCall = canCall;
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;
            AlertLevels = alertLevels;
            CurrentAlert = currentAlert;
            CurrentAlertDelay = currentAlertDelay;
            AdditionalAlertLevels = additionalAlertLevels ?? []; // Sunrise-Edit
            // Sunrise-Start
            CanRelay = canRelay;
            IsRelaying = isRelaying;
            RelayCooldownRemaining = relayCooldownRemaining;
            RelayTimeRemaining = relayTimeRemaining;
            // Sunrise-End
        }
    }

    // Sunrise added start - сетевой контракт дополнительных кодов
    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleAdditionalAlertLevelState
    {
        public readonly string Level;
        public readonly bool Enabled;
        public readonly bool Selectable;

        public CommunicationsConsoleAdditionalAlertLevelState(string level, bool enabled, bool selectable)
        {
            Level = level;
            Enabled = enabled;
            Selectable = selectable;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSetAdditionalAlertLevelMessage : BoundUserInterfaceMessage
    {
        public readonly string Level;
        public readonly bool Enabled;

        public CommunicationsConsoleSetAdditionalAlertLevelMessage(string level, bool enabled)
        {
            Level = level;
            Enabled = enabled;
        }
    }
    // Sunrise added end

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSelectAlertLevelMessage : BoundUserInterfaceMessage
    {
        public readonly string Level;

        public CommunicationsConsoleSelectAlertLevelMessage(string level)
        {
            Level = level;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleAnnounceMessage : BoundUserInterfaceMessage
    {
        public readonly string Message;

        public CommunicationsConsoleAnnounceMessage(string message)
        {
            Message = message;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleBroadcastMessage : BoundUserInterfaceMessage
    {
        public readonly string Message;
        public CommunicationsConsoleBroadcastMessage(string message)
        {
            Message = message;
        }
    }

    // Sunrise-Start
    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleToggleRelayMessage : BoundUserInterfaceMessage
    {
    }
    // Sunrise-End

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleCallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleRecallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CommunicationsConsoleUiKey
    {
        Key
    }
}
