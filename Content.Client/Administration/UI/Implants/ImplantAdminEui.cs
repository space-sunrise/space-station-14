using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.Implants;

[UsedImplicitly]
public sealed class ImplantAdminEui : BaseEui
{
    private readonly ImplantAdminWindow _window;

    public ImplantAdminEui()
    {
        _window = new ImplantAdminWindow();
        _window.OnClose += OnClosed;
        _window.OnAddImplant += id => SendMessage(new ImplantAdminAddMessage { ProtoId = id });
        _window.OnRemoveImplant += uid => SendMessage(new ImplantAdminRemoveMessage { ImplantNetEntity = uid });
        _window.OnReplaceBodySlot += (slot, isOrgan, proto) =>
            SendMessage(new ImplantAdminReplaceBodySlotMessage { SlotId = slot, IsOrgan = isOrgan, ProtoId = proto });
        _window.OnRemoveBodySlot += (slot, isOrgan) =>
            SendMessage(new ImplantAdminRemoveBodySlotMessage { SlotId = slot, IsOrgan = isOrgan });
    }

        private void OnClosed()
        {
            SendMessage(new CloseEuiMessage());
        }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (ImplantAdminEuiState) state;
        _window.SetState(s);
    }
}
