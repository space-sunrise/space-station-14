using Content.Client.Administration.Managers;
using Content.Shared.Sandbox;
using Robust.Shared.Player;

namespace Content.Client.Sandbox
{

    public partial class SandboxSystem : SharedSandboxSystem
    {
        public bool ThermalVisionActive { get; private set; }
        public event Action? ThermalVisionChanged;

        public partial void ThermalVision()
        {
            ThermalVisionActive = !ThermalVisionActive;
            ThermalVisionChanged?.Invoke();
            RaiseNetworkEvent(new MsgSandboxThermalVision());
        }
    }
}

