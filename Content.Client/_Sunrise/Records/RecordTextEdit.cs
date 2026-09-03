using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Records;

/// <summary>
/// Многострочное поле досье с явным фоном и подсветкой фокуса.
/// </summary>
public sealed class RecordTextEdit : PanelContainer
{
    private static readonly StyleBoxFlat NormalStyle = new()
    {
        BackgroundColor = Color.FromHex("#17171B"),
        BorderColor = Color.FromHex("#45454D"),
        BorderThickness = new Thickness(1),
    };

    private static readonly StyleBoxFlat FocusedStyle = new()
    {
        BackgroundColor = Color.FromHex("#19191E"),
        BorderColor = Color.FromHex("#9B4DB5"),
        BorderThickness = new Thickness(1),
    };

    public readonly TextEdit Input;
    private bool _focused;

    public Rope.Node TextRope
    {
        get => Input.TextRope;
        set => Input.TextRope = value;
    }

    public event Action<TextEdit.TextEditEventArgs>? OnTextChanged;

    public RecordTextEdit()
    {
        PanelOverride = NormalStyle;
        RectClipContent = true;

        Input = new TextEdit
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(7, 6),
        };
        Input.OnTextChanged += args => OnTextChanged?.Invoke(args);
        AddChild(Input);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var focused = Input.HasKeyboardFocus();
        if (_focused == focused)
            return;

        _focused = focused;
        PanelOverride = focused ? FocusedStyle : NormalStyle;
    }
}
