using Content.Server._Sunrise.UploadedContent;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Content.Tests.Server._Sunrise.UploadedContent;

[TestFixture]
[TestOf(typeof(UploadedContentCatalog))]
public sealed class UploadedContentCatalogTest
{
    [Test]
    public void RepeatedPathReplacesSizeWithoutChangingOrderOrCount()
    {
        var catalog = new UploadedContentCatalog();
        catalog.AddOrUpdate(new ResPath("Audio/_Sunrise/first.ogg"), 100);
        catalog.AddOrUpdate(new ResPath("Audio/_Sunrise/second.ogg"), 200);
        catalog.AddOrUpdate(new ResPath("/Audio/_Sunrise/first.ogg"), 350);

        var manifest = catalog.CreateManifest();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.TotalBytes, Is.EqualTo(550));
            Assert.That(manifest.Files[0].Path, Is.EqualTo(new ResPath("Audio/_Sunrise/first.ogg")));
            Assert.That(manifest.Files[0].SizeBytes, Is.EqualTo(350));
            Assert.That(manifest.Files[1].Path, Is.EqualTo(new ResPath("Audio/_Sunrise/second.ogg")));
            Assert.That(manifest.Files[1].SizeBytes, Is.EqualTo(200));
        });
    }
}
