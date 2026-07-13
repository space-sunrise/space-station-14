using Content.Client.DoAfter;
using NUnit.Framework;

namespace Content.Tests.Client.DoAfter;

[TestFixture]
[TestOf(typeof(DoAfterOverlay))]
public sealed class DoAfterOverlayTest
{
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, true)]
    public void TestDoAfterVisibility(bool hidden, bool hideDoAfter, bool expected)
    {
        Assert.That(DoAfterOverlay.ShouldHideDoAfter(hidden, hideDoAfter), Is.EqualTo(expected));
    }
}
