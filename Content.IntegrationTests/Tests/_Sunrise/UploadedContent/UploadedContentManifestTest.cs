using Content.Client._Sunrise.UploadedContent;
using Content.Server._Sunrise.UploadedContent;
using Content.Shared._Sunrise.UploadedContent;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Sunrise.UploadedContent;

[TestFixture]
[TestOf(typeof(UploadedContentManifestManager))]
public sealed class UploadedContentManifestTest
{
    [Test]
    public async Task ClientReceivesEmptyManifestAndSequentialFullSnapshots()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Fresh = true,
            Destructive = true,
        });

        var clientProgress = pair.Client.ResolveDependency<UploadedContentProgressManager>();
        var serverManifest = pair.Server.ResolveDependency<UploadedContentManifestManager>();

        await pair.RunTicksSync(1);
        Assert.Multiple(() =>
        {
            Assert.That(clientProgress.Snapshot.ManifestReceived, Is.True);
            Assert.That(clientProgress.Snapshot.TotalFiles, Is.Zero);
            Assert.That(clientProgress.Snapshot.IsComplete, Is.True);
        });

        await pair.Server.WaitPost(() => serverManifest.ApplyUploadedResources(
        [
            new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/first.ogg"), 100),
        ]));
        await pair.RunTicksSync(1);

        Assert.Multiple(() =>
        {
            Assert.That(clientProgress.Snapshot.TotalFiles, Is.EqualTo(1));
            Assert.That(clientProgress.Snapshot.TotalBytes, Is.EqualTo(100));
        });

        await pair.Server.WaitPost(() => serverManifest.ApplyUploadedResources(
        [
            new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/first.ogg"), 150),
            new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/second.ogg"), 250),
        ]));
        await pair.RunTicksSync(1);

        Assert.Multiple(() =>
        {
            Assert.That(clientProgress.Snapshot.TotalFiles, Is.EqualTo(2));
            Assert.That(clientProgress.Snapshot.TotalBytes, Is.EqualTo(400));
        });

        await pair.CleanReturnAsync();
    }
}
