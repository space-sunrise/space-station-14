using System;
using Content.Shared._Sunrise.UploadedContent;
using Lidgren.Network;
using NUnit.Framework;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Tests.Shared._Sunrise.UploadedContent;

[TestFixture]
[TestOf(typeof(MsgUploadedContentManifest))]
public sealed class MsgUploadedContentManifestTest
{
    [Test]
    public void EmptyManifestRoundTrips()
    {
        var result = RoundTrip(new MsgUploadedContentManifest());

        Assert.Multiple(() =>
        {
            Assert.That(result.MsgGroup, Is.EqualTo(MsgGroups.EntityEvent));
            Assert.That(result.Files, Is.Empty);
        });
    }

    [Test]
    public void FilledManifestRoundTripsInOrder()
    {
        var message = new MsgUploadedContentManifest
        {
            Files =
            [
                new UploadedContentManifestEntry(new ResPath("/Audio/_Sunrise/first.ogg"), 128),
                new UploadedContentManifestEntry(new ResPath("Textures/_Sunrise/second.png"), 4096),
            ],
        };

        var result = RoundTrip(message);

        Assert.That(result.Files, Is.EqualTo(new[]
        {
            new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/first.ogg"), 128),
            new UploadedContentManifestEntry(new ResPath("Textures/_Sunrise/second.png"), 4096),
        }));
    }

    private static MsgUploadedContentManifest RoundTrip(MsgUploadedContentManifest source)
    {
        var outgoing = CreateLidgrenMessage<NetOutgoingMessage>();
        source.WriteToBuffer(outgoing, null!);

        var incoming = CreateLidgrenMessage<NetIncomingMessage>();
        incoming.Data = outgoing.Data.AsSpan(0, outgoing.LengthBytes).ToArray();
        incoming.LengthBits = outgoing.LengthBits;

        var result = new MsgUploadedContentManifest();
        result.ReadFromBuffer(incoming, null!);
        return result;
    }

    private static T CreateLidgrenMessage<T>() where T : class
    {
        return (T)(Activator.CreateInstance(typeof(T), nonPublic: true)
                   ?? throw new InvalidOperationException($"Не удалось создать {typeof(T).Name}."));
    }
}
