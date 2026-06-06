using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics;

namespace Content.Client._Sunrise.Tutorial.TutorialBubbleControl;

public sealed class TutorialBubbleTail : Control
{
    private Color _tailColor = Color.White;

    public Color TailColor
    {
        get => _tailColor;
        set
        {
            if (_tailColor == value)
                return;

            _tailColor = value;
            InvalidateArrange();
        }
    }
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var width = PixelSize.X;
        var height = PixelSize.Y;
        if (width <= 0 || height <= 0)
            return;

        var verts = new Vector2[3];
        verts[0] = new Vector2(0f, 0f);
        verts[1] = new Vector2(width, 0f);
        verts[2] = new Vector2(width / 2f, height);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, verts.AsSpan(), _tailColor);
    }
}
