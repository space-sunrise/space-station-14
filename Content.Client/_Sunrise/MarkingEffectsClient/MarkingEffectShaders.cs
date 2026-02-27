using System.Numerics;
using Content.Shared._Sunrise.MarkingEffects;
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

                // Safely get colors with fallback for old imported characters
                SetColors(gradient, instance);

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

                // Safely get colors with fallback for old imported characters
                SetColors(roughGradient, instance);

                instance.SetParameter("horizontal", roughGradient.Horizontal);
                break;
        }
    }

    private static void SetColors(MarkingEffect effect, ShaderInstance instance)
    {
        var baseColor2 = effect.Colors.TryGetValue("base", out var bColor2) ? bColor2 : Color.White;
        var gradientColor2 = effect.Colors.TryGetValue("gradient", out var gColor2) ? gColor2 : baseColor2;

        instance.SetParameter("color1", ColorToVec(baseColor2));
        instance.SetParameter("color2", ColorToVec(gradientColor2));
    }
}
