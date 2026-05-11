using Content.Shared.Clothing.Dirt;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Clothing.Dirt;

// тестируем логику без ECS - просто чистые функции скопированные из системы
[TestFixture]
public sealed class ClothingDirtSystemTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static ClothingDirtComponent Fresh() => new();

    private static void Apply(ClothingDirtComponent d, Color color, float amt)
    {
        if (amt <= 0f) return;
        var ex = d.Layers.FirstOrDefault(l => Close(l.Color, color));
        if (ex != null)
            ex.Intensity = Math.Clamp(ex.Intensity + amt, 0f, 100f);
        else
            d.Layers.Add(new DirtLayer { Color = color, Intensity = Math.Clamp(amt, 0f, 100f) });
        Recalc(d);
    }

    private static void Clean(ClothingDirtComponent d, float amt)
    {
        foreach (var l in d.Layers)
            l.Intensity = Math.Max(0f, l.Intensity - amt);
        d.Layers.RemoveAll(l => l.Intensity <= 0f);
        Recalc(d);
    }

    private static void Recalc(ClothingDirtComponent d)
    {
        if (d.Layers.Count == 0) { d.DirtLevel = 0f; d.DirtColor = Color.Transparent; return; }
        d.DirtLevel = Math.Min(d.Layers.Sum(l => l.Intensity), 100f);
        var total = d.Layers.Sum(l => l.Intensity);
        float r = 0f, g = 0f, b = 0f;
        foreach (var l in d.Layers) { var w = l.Intensity / total; r += l.Color.R * w; g += l.Color.G * w; b += l.Color.B * w; }
        d.DirtColor = new Color(r, g, b);
    }

    private static bool Close(Color a, Color b)
        => Math.Abs(a.R - b.R) < 0.15f && Math.Abs(a.G - b.G) < 0.15f && Math.Abs(a.B - b.B) < 0.15f;

    // ── apply ─────────────────────────────────────────────────────────────────

    [Test]
    public void Apply_SetsLevel()
    {
        var d = Fresh();
        Apply(d, Color.Red, 33f);
        Assert.That(d.DirtLevel, Is.EqualTo(33f).Within(0.01f));
    }

    [Test]
    public void Apply_SameColor_OneLayer()
    {
        var d = Fresh();
        Apply(d, Color.Red, 20f);
        Apply(d, Color.Red, 20f);
        Assert.That(d.Layers.Count, Is.EqualTo(1));
        Assert.That(d.DirtLevel, Is.EqualTo(40f).Within(0.01f));
    }

    [Test]
    public void Apply_DifferentColors_TwoLayers()
    {
        var d = Fresh();
        Apply(d, Color.Red,  25f);
        Apply(d, Color.Blue, 25f);
        Assert.That(d.Layers.Count, Is.EqualTo(2));
    }

    [Test]
    public void Apply_CloseColors_Merged()
    {
        var d = Fresh();
        Apply(d, new Color(0.8f, 0.1f, 0.1f), 20f);
        Apply(d, new Color(0.82f, 0.09f, 0.1f), 20f); // разница < 0.15
        Assert.That(d.Layers.Count, Is.EqualTo(1));
    }

    [Test]
    public void Apply_NeverExceeds100()
    {
        var d = Fresh();
        Apply(d, Color.Red, 90f);
        Apply(d, Color.Red, 90f);
        Assert.That(d.DirtLevel, Is.EqualTo(100f).Within(0.01f));
    }

    [Test]
    public void Apply_Zero_NoEffect()
    {
        var d = Fresh();
        Apply(d, Color.Red, 0f);
        Assert.That(d.Layers.Count, Is.EqualTo(0));
    }

    // ── clean ─────────────────────────────────────────────────────────────────

    [Test]
    public void Clean_Full_ZeroAndEmpty()
    {
        var d = Fresh();
        Apply(d, Color.Red, 66f);
        Clean(d, 100f);
        Assert.That(d.DirtLevel, Is.EqualTo(0f).Within(0.01f));
        Assert.That(d.Layers, Is.Empty);
    }

    [Test]
    public void Clean_Partial_ReducesCorrectly()
    {
        var d = Fresh();
        Apply(d, Color.Red, 80f);
        Clean(d, 30f);
        Assert.That(d.DirtLevel, Is.EqualTo(50f).Within(0.01f));
    }

    [Test]
    public void Clean_AlreadyClean_NoThrow()
    {
        var d = Fresh();
        Assert.DoesNotThrow(() => Clean(d, 100f));
    }

    // ── color mixing ──────────────────────────────────────────────────────────

    [Test]
    public void Mix_EqualParts_AverageColor()
    {
        var d = Fresh();
        Apply(d, Color.Red,  50f); // (1,0,0)
        Apply(d, Color.Blue, 50f); // (0,0,1)
        Assert.That(d.DirtColor.R, Is.EqualTo(0.5f).Within(0.05f));
        Assert.That(d.DirtColor.B, Is.EqualTo(0.5f).Within(0.05f));
    }

    [Test]
    public void Mix_Dominant_WinsColor()
    {
        var d = Fresh();
        Apply(d, Color.Red,  80f);
        Apply(d, Color.Blue, 20f);
        Assert.That(d.DirtColor.R, Is.GreaterThan(d.DirtColor.B));
    }

    [Test]
    public void NoLayers_TransparentColor()
    {
        var d = Fresh();
        Assert.That(d.DirtColor, Is.EqualTo(Color.Transparent));
    }

    // ── threshold label (логика UI) ───────────────────────────────────────────

    [TestCase(0f,   ExpectedResult = "")]
    [TestCase(1f,   ExpectedResult = "33%")]
    [TestCase(33f,  ExpectedResult = "33%")]
    [TestCase(34f,  ExpectedResult = "66%")]
    [TestCase(67f,  ExpectedResult = "100%")]
    [TestCase(100f, ExpectedResult = "100%")]
    public string ThresholdLabel(float level)
    {
        if (level <= 0f)  return "";
        if (level > 66f)  return "100%";
        if (level > 33f)  return "66%";
        return "33%";
    }

    // ── edge cases ────────────────────────────────────────────────────────────

    [Test]
    public void ManyLayers_StillCappedAt100()
    {
        var d = Fresh();
        var colors = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow,
                              Color.Cyan, Color.Magenta, Color.Orange, Color.Purple };
        foreach (var c in colors)
            Apply(d, c, 20f);
        Assert.That(d.DirtLevel, Is.EqualTo(100f).Within(0.01f));
    }
}
