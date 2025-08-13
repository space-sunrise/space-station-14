using System.Numerics;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Sunrise.MarkingEffectsClient;

public static class MarkingEffectShaders
{

    public static Vector3 ColorToVec(Color col)
    {
        return new Vector3(col.R, col.G, col.B);
    }

    public static void ApplyShaderParams(this ShaderInstance instance, MarkingEffect color, Vector2 texScale)
    {
        instance.SetParameter("useDisplacement", false);

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
            case MarkingEffectType.RoughGradient:
                if (color is not RoughGradientMarkingEffect roughGradient)
                    return;
                instance.SetParameter("color1", ColorToVec(roughGradient.Colors["base"]));
                instance.SetParameter("color2", ColorToVec(roughGradient.Colors["gradient"]));
                instance.SetParameter("horizontal", roughGradient.Horizontal);
                break;
        }
    }
}
