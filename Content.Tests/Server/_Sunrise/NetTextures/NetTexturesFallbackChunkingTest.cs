using System.Linq;
using Content.Shared._Sunrise.NetTextures;
using NUnit.Framework;

namespace Content.Tests.Server._Sunrise.NetTextures;

[TestFixture]
[TestOf(typeof(Content.Server._Sunrise.NetTexturesManager))]
[Parallelizable(ParallelScope.All)]
public sealed class NetTexturesFallbackChunkingTest
{
    [Test]
    public void CreateFallbackChunks_RoundTripsPayload()
    {
        var relativePath = new Robust.Shared.Utility.ResPath("NetTextures/Test/chunked.png");
        var payload = Enumerable.Range(0, 100_000)
            .Select(i => (byte) (i % 251))
            .ToArray();

        var chunks = Content.Server._Sunrise.NetTexturesManager
            .CreateFallbackChunks(relativePath, payload, chunkSize: 32 * 1024)
            .ToArray();

        Assert.That(chunks, Has.Length.EqualTo(4));
        Assert.That(chunks.Select(chunk => chunk.RelativePath).Distinct().Single(), Is.EqualTo(relativePath.ToString()));
        Assert.That(chunks.Select(chunk => chunk.TotalChunks).Distinct().Single(), Is.EqualTo(chunks.Length));
        Assert.That(chunks.Select(chunk => chunk.TotalLength).Distinct().Single(), Is.EqualTo(payload.Length));
        Assert.That(chunks.Select(chunk => chunk.ChunkIndex), Is.EqualTo(Enumerable.Range(0, chunks.Length)));
        Assert.That(chunks.Select(chunk => chunk.ChunkOffset), Is.EqualTo(new[] { 0, 32 * 1024, 64 * 1024, 96 * 1024 }));

        var rebuilt = chunks
            .OrderBy(chunk => chunk.ChunkIndex)
            .SelectMany(chunk => chunk.Data)
            .ToArray();

        Assert.That(rebuilt, Is.EqualTo(payload));
        Assert.That(chunks[0].Data.Length, Is.EqualTo(32 * 1024));
        Assert.That(chunks[^1].Data.Length, Is.EqualTo(payload.Length - (32 * 1024 * 3)));
    }
}
