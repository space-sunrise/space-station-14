using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Sunrise.UserInterface.CustomControls;

public sealed class PhotoPreviewWindow : DefaultWindow
{
    public PhotoPreviewWindow(Texture texture)
    {
        Title = Loc.GetString("messenger-image-preview-title");
        MinSize = new Vector2(600, 600);

        var textureRect = new TextureRect
        {
            Texture = texture,
            Stretch = TextureRect.StretchMode.KeepAspect,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        Contents.AddChild(textureRect);
    }

    public static void Open(Texture? texture)
    {
        if (texture == null)
            return;

        var window = new PhotoPreviewWindow(texture);
        window.OpenCentered();
    }
}
