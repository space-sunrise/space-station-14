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

                // Safely get colors with fallback for old imported characters
                var baseColor = gradient.Colors.TryGetValue("base", out var bColor) ? bColor : Color.White;
                var gradientColor = gradient.Colors.TryGetValue("gradient", out var gColor) ? gColor : baseColor;

                instance.SetParameter("color1", ColorToVec(baseColor));
                instance.SetParameter("color2", ColorToVec(gradientColor));
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
                var baseColor2 = roughGradient.Colors.TryGetValue("base", out var bColor2) ? bColor2 : Color.White;
                var gradientColor2 = roughGradient.Colors.TryGetValue("gradient", out var gColor2) ? gColor2 : baseColor2;

                instance.SetParameter("color1", ColorToVec(baseColor2));
                instance.SetParameter("color2", ColorToVec(gradientColor2));
                instance.SetParameter("horizontal", roughGradient.Horizontal);
                break;
        }
    }
}
