using System.Numerics;
using Robust.Client.Graphics;

#pragma warning disable IDE0130 // Пространство имён совпадает с расширяемой vanilla-подсистемой.
namespace Content.Client.Parallax;

/// <summary>
/// Отрисовывает слой параллакса с поворотом самой геометрии, не обрезая текстуру исходным прямоугольником.
/// </summary>
internal static class ParallaxDrawingHelpers
{
    public static void DrawTextureRect(
        DrawingHandleWorld handle,
        Texture texture,
        Box2 bounds,
        Angle rotation)
    {
        if (rotation == Angle.Zero)
        {
            handle.DrawTextureRect(texture, bounds);
            return;
        }

        var rotatedBounds = new Box2Rotated(bounds, rotation, bounds.Center);
        handle.DrawTextureRect(texture, in rotatedBounds);
    }

    public static void DrawTextureRect(
        DrawingHandleScreen handle,
        Texture texture,
        UIBox2 bounds,
        Angle rotation)
    {
        if (rotation == Angle.Zero)
        {
            handle.DrawTextureRect(texture, bounds);
            return;
        }

        var oldTransform = handle.GetTransform();
        var center = bounds.Center;
        var rotationTransform =
            Matrix3x2.CreateTranslation(-center) *
            Matrix3Helpers.CreateRotation(rotation.Theta) *
            Matrix3x2.CreateTranslation(center);

        handle.SetTransform(rotationTransform * oldTransform);
        try
        {
            handle.DrawTextureRect(texture, bounds);
        }
        finally
        {
            handle.SetTransform(oldTransform);
        }
    }
}
