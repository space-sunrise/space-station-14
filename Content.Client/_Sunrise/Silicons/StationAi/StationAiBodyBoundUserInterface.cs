using Content.Shared._Sunrise.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Silicons.StationAi;

public sealed class StationAiBodyBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private StationAiBodyWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StationAiBodyWindow>();
        _window.EnterBodyAction += EnterBody;
        _window.ExitBodyAction += ExitBody;
        Update();
    }

    public override void Update()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent<StationAiBodyControllerComponent>(Owner, out var controller))
            return;

        _window.UpdateState(controller);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _window == null)
            return;

        _window.EnterBodyAction -= EnterBody;
        _window.ExitBodyAction -= ExitBody;
    }

    private void EnterBody(NetEntity body)
    {
        SendPredictedMessage(new StationAiBodyEnterMessage(body));
    }

    private void ExitBody()
    {
        SendPredictedMessage(new StationAiBodyExitMessage());
    }
}
