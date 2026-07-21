using Content.Client.Eui;
using Content.Shared._Sunrise.Antag.UI;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.Antag.UI;

[UsedImplicitly]
public sealed class AntagNameEui : BaseEui
{
    private readonly AntagNameWindow _window;
    private bool _closing;

    public AntagNameEui()
    {
        _window = new AntagNameWindow();

        _window.NameConfirmed += name =>
        {
            SendMessage(new AntagNameSelectedMessage(name, false));
        };

        _window.RandomKept += () =>
        {
            SendMessage(new AntagNameSelectedMessage(null, true));
        };

        _window.OnClose += () =>
        {
            if (_closing)
                return;

            SendMessage(new CloseEuiMessage());
        };
    }

    public override void Opened()
    {
        base.Opened();

        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _closing = true;
        _window.Close();
        _closing = false;
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is AntagNameEuiState antagNameState)
            _window.SetState(antagNameState);
    }
}
