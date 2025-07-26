using System.Numerics;
using Content.Shared._Sunrise.ExtendedColor;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Sunrise.MarkingEffects;

public static class MarkingEffectShaders
{

    public static Robust.Shared.Maths.Vector3 ColorToVec(Color col)
    {
        return new Robust.Shared.Maths.Vector3(col.R, col.G, col.B);
    }

    public static void ApplyShaderParams(this ShaderInstance instance, MarkingEffect color, Vector2 texScale)
    {

        switch (color.Type)
        {
            case MarkingEffectType.Gradient:
                if (color is not GradientMarkingEffect gradient)
                    return;

                instance.SetParameter("color1", ColorToVec(gradient.Colors["base"]));
                instance.SetParameter("color2", ColorToVec(gradient.Colors["gradient"]));
                instance.SetParameter("texScale", texScale);
                instance.SetParameter("offset", gradient.Offset);
                instance.SetParameter("size", gradient.Size);
                instance.SetParameter("rotation", gradient.Rotation);
                instance.SetParameter("pixelated", gradient.Pixelated);
                instance.SetParameter("mirrored", gradient.Mirrored);
                break;
        }
    }
}
