using Content.Client._Sunrise.Paint.UI;
using Content.Shared._Sunrise.Paint;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Paint;

public sealed class PaintBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    protected override void Open()
    {
        base.Open();

        var window = this.CreateWindow<PaintColorWindow>();
        window.OnApplyButtonPressed += OnApplyButtonPressed;
        if (EntMan.TryGetComponent(Owner, out PaintComponent? paint))
            window.Color = paint.Color;
    }

    private void OnApplyButtonPressed(Color color)
    {
        SendPredictedMessage(new PaintSetColorMessage(color));
    }
}
