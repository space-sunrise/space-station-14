using Content.Client._Sunrise.Sandbox.Access.Overlays;
using Content.Client.Clickable;
using NUnit.Framework;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Tests.Client._Sunrise.Sandbox.Access;

[TestFixture]
public sealed class MappingAccessTightBoundsTest
{
    [Test]
    public void TryGetOpaqueLocalBounds_CroppedShape_ReturnsExpectedBounds()
    {
        var img = new Image<Rgba32>(4, 4);
        img[1, 1] = new Rgba32(0, 0, 0, 255);
        img[2, 1] = new Rgba32(0, 0, 0, 255);
        img[1, 2] = new Rgba32(0, 0, 0, 255);
        img[2, 2] = new Rgba32(0, 0, 0, 255);

        var clickMap = ClickMapManager.ClickMap.FromImage(img, 0.5f);
        var result = MappingAccessTightBounds.TryGetOpaqueLocalBounds(
            new Vector2i(img.Width, img.Height),
            clickMap.IsOccluded,
            out var bounds);

        Assert.That(result, Is.True);
        AssertBoxEquals(bounds, new Box2(-1f / 32f, -1f / 32f, 1f / 32f, 1f / 32f));
    }

    [Test]
    public void TryGetOpaqueLocalBounds_SinglePixel_ReturnsSinglePixelBounds()
    {
        var img = new Image<Rgba32>(4, 4);
        img[3, 0] = new Rgba32(0, 0, 0, 255);

        var clickMap = ClickMapManager.ClickMap.FromImage(img, 0.5f);
        var result = MappingAccessTightBounds.TryGetOpaqueLocalBounds(
            new Vector2i(img.Width, img.Height),
            clickMap.IsOccluded,
            out var bounds);

        Assert.That(result, Is.True);
        AssertBoxEquals(bounds, new Box2(1f / 32f, 1f / 32f, 2f / 32f, 2f / 32f));
    }

    [Test]
    public void TryGetOpaqueLocalBounds_TransparentImage_ReturnsFalse()
    {
        var img = new Image<Rgba32>(4, 4);
        var clickMap = ClickMapManager.ClickMap.FromImage(img, 0.5f);

        var result = MappingAccessTightBounds.TryGetOpaqueLocalBounds(
            new Vector2i(img.Width, img.Height),
            clickMap.IsOccluded,
            out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetOpaqueLocalBounds_OffsetShape_ReturnsExpectedBounds()
    {
        var img = new Image<Rgba32>(6, 6);
        img[0, 3] = new Rgba32(0, 0, 0, 255);
        img[1, 3] = new Rgba32(0, 0, 0, 255);
        img[0, 4] = new Rgba32(0, 0, 0, 255);
        img[1, 4] = new Rgba32(0, 0, 0, 255);

        var clickMap = ClickMapManager.ClickMap.FromImage(img, 0.5f);
        var result = MappingAccessTightBounds.TryGetOpaqueLocalBounds(
            new Vector2i(img.Width, img.Height),
            clickMap.IsOccluded,
            out var bounds);

        Assert.That(result, Is.True);
        AssertBoxEquals(bounds, new Box2(-3f / 32f, -2f / 32f, -1f / 32f, 0f));
    }

    private static void AssertBoxEquals(Box2 actual, Box2 expected)
    {
        const float epsilon = 0.0001f;

        Assert.That(actual.Left, Is.EqualTo(expected.Left).Within(epsilon));
        Assert.That(actual.Bottom, Is.EqualTo(expected.Bottom).Within(epsilon));
        Assert.That(actual.Right, Is.EqualTo(expected.Right).Within(epsilon));
        Assert.That(actual.Top, Is.EqualTo(expected.Top).Within(epsilon));
    }
}
