using Content.Client._Sunrise.CentCom.UI;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.CentCom.BUI;

[UsedImplicitly]
public sealed class CentComCargoConsoleBoundUserInterface : BoundUserInterface
{
    private CentComCargoConsoleWindow? _window;
    private EntityUid? _owner;

    public CentComCargoConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CentComCargoConsoleWindow>();

        _window.OnSendGifts += WindowOnOnSendGifts;

        _window.OpenCentered();

        if (!EntMan.TryGetComponent(_owner, out CentComCargoConsoleComponent? component))
            return;

        foreach (var i in component.Gifts)
        {
            _window.AddElement(i.Title, i.Description, i.Contents, i.Event);
        }
    }

    private void WindowOnOnSendGifts(string? obj)
    {
        if (obj == null)
        {
            return;
        }

        SendMessage(new CentComCargoSendGiftMessage(obj));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
