using Content.Shared._Sunrise.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Silicons.StationAi;

public sealed class StationAiBodyBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private StationAiBodyWindow? _window;
    private StationAiBodyBuiState? _lastState;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StationAiBodyWindow>();
        _window.EnterBodyAction += EnterBody;
        _window.ExitBodyAction += ExitBody;

        if (State is StationAiBodyBuiState state)
            ApplyState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not StationAiBodyBuiState bodyState ||
            IsSameState(_lastState, bodyState))
        {
            return;
        }

        ApplyState(bodyState);
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

    private void ApplyState(StationAiBodyBuiState state)
    {
        _lastState = state;
        _window?.UpdateState(state);
    }

    private static bool IsSameState(StationAiBodyBuiState? left, StationAiBodyBuiState right)
    {
        if (left == null ||
            left.CurrentBody != right.CurrentBody ||
            left.Bodies.Count != right.Bodies.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Bodies.Count; i++)
        {
            var leftBody = left.Bodies[i];
            var rightBody = right.Bodies[i];

            if (leftBody.Body != rightBody.Body ||
                leftBody.BodyNumber != rightBody.BodyNumber ||
                leftBody.Name != rightBody.Name ||
                leftBody.LinkedAi != rightBody.LinkedAi ||
                leftBody.IsCurrent != rightBody.IsCurrent)
            {
                return false;
            }
        }

        return true;
    }
}
