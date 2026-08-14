using Content.Shared._Sunrise.Research.TechnologyDisk;
using Content.Shared.Research.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Research.TechnologyDisk.UI;

public sealed class SunriseDiskConsoleBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private SunriseDiskConsoleMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SunriseDiskConsoleMenu>();
        _menu.OnServerButtonPressed += () => SendMessage(new ConsoleServerSelectionMessage());
        _menu.OnPrintButtonPressed += prototype =>
            SendMessage(new SunriseDiskConsolePrintDiskMessage(prototype));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SunriseDiskConsoleBoundUserInterfaceState sunriseState)
            _menu?.Update(sunriseState);
    }
}
