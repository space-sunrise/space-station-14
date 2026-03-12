using Content.Shared._Sunrise.CodeConsole;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.CodeConsole;

[UsedImplicitly]
public sealed class CodeConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CodeConsoleMenu? _menu;

    public CodeConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CodeConsoleMenu>();

        _menu.OnKeypadButtonPressed += i =>
        {
            SendMessage(new CodeConsoleKeypadMessage(i));
        };
        _menu.OnEnterButtonPressed += () =>
        {
            SendMessage(new CodeConsoleKeypadEnterMessage());
        };
        _menu.OnClearButtonPressed += () =>
        {
            SendMessage(new CodeConsoleKeypadClearMessage());
        };

        _menu.ActivateButton.OnPressed += _ =>
        {
            SendMessage(new CodeConsoleActivateButtonMessage());
        };
        _menu.LockButton.OnPressed += _ =>
        {
            SendMessage(new CodeConsoleLockButtonMessage());
        };

        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        switch (state)
        {
            case CodeConsoleUiState msg:
                _menu.UpdateState(msg);
                break;
        }
    }
}
