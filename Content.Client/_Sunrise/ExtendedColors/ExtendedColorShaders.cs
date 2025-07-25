using System.Numerics;
using Content.Shared._Sunrise.ExtendedColor;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Sunrise.ExtendedColors;

public static class ExtendedColorShaders
{

    public static Robust.Shared.Maths.Vector3 ColorToVec(Color col)
    {
        return new Robust.Shared.Maths.Vector3(col.R, col.G, col.B);
    }

    public static void ApplyShaderParams(this ShaderInstance instance, ExtendedColor color, Vector2 texScale)
    {

        switch (color.Type)
        {
            case ColorType.Gradient:
                instance.SetParameter("color1", ColorToVec(color.Colors["base"]));
                instance.SetParameter("color2", ColorToVec(color.Colors["gradient"]));
                instance.SetParameter("texScale", texScale);
                instance.SetParameter("offset", color.Offset);
                instance.SetParameter("size", color.Size);
                instance.SetParameter("rotation", color.Rotation);
                instance.SetParameter("pixelated", color.Pixelated);
                instance.SetParameter("mirrored", color.Mirrored);
                break;
        }
    }
}
