using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Database;
using Moq;
using NUnit.Framework;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.Tests.Server._Sunrise.Administration.Managers;

[TestFixture]
public sealed class BanManagerWebhookTests
{
    [Test]
    public async Task IpOnlyBanUsesAddressLookup()
    {
        var address = IPAddress.Parse("203.0.113.10");
        var ban = CreateBan(address: (address, 32));
        var persistedBan = CreateBan(id: 10, address: (address, 32));
        var db = new Mock<IServerDbManager>();
        db.Setup(d => d.GetServerBanAsync(address, null, null, null))
            .ReturnsAsync(persistedBan);

        var resolved = await BanManager.ResolveServerBanForWebhook(db.Object, ban, CreateSawmill());

        Assert.That(resolved, Is.SameAs(persistedBan));
        db.Verify(d => d.GetServerBanAsync(address, null, null, null), Times.Once);
    }

    [Test]
    public async Task LegacyHwidOnlyBanUsesLegacyHwidLookup()
    {
        var hwId = ImmutableArray.Create<byte>(1, 2, 3);
        var ban = CreateBan(hwid: new ImmutableTypedHwid(hwId, HwidType.Legacy));
        var persistedBan = CreateBan(id: 11, hwid: new ImmutableTypedHwid(hwId, HwidType.Legacy));
        var db = new Mock<IServerDbManager>();
        db.Setup(d => d.GetServerBanAsync(
                null,
                null,
                It.Is<ImmutableArray<byte>?>(value => Matches(value, hwId)),
                null))
            .ReturnsAsync(persistedBan);

        var resolved = await BanManager.ResolveServerBanForWebhook(db.Object, ban, CreateSawmill());

        Assert.That(resolved, Is.SameAs(persistedBan));
        db.Verify(d => d.GetServerBanAsync(
            null,
            null,
            It.Is<ImmutableArray<byte>?>(value => Matches(value, hwId)),
            null), Times.Once);
    }

    [Test]
    public async Task ModernHwidOnlyBanUsesModernHwidLookup()
    {
        var hwId = ImmutableArray.Create<byte>(4, 5, 6);
        var ban = CreateBan(hwid: new ImmutableTypedHwid(hwId, HwidType.Modern));
        var persistedBan = CreateBan(id: 12, hwid: new ImmutableTypedHwid(hwId, HwidType.Modern));
        var db = new Mock<IServerDbManager>();
        db.Setup(d => d.GetServerBanAsync(
                null,
                null,
                null,
                It.Is<ImmutableArray<ImmutableArray<byte>>?>(value => MatchesModern(value, hwId))))
            .ReturnsAsync(persistedBan);

        var resolved = await BanManager.ResolveServerBanForWebhook(db.Object, ban, CreateSawmill());

        Assert.That(resolved, Is.SameAs(persistedBan));
        db.Verify(d => d.GetServerBanAsync(
            null,
            null,
            null,
            It.Is<ImmutableArray<ImmutableArray<byte>>?>(value => MatchesModern(value, hwId))), Times.Once);
    }

    [Test]
    public async Task MissingLookupFallsBackToOriginalBan()
    {
        var userId = new NetUserId(Guid.NewGuid());
        var ban = CreateBan(userId: userId);
        var db = new Mock<IServerDbManager>();
        db.Setup(d => d.GetServerBanAsync(null, userId, null, null))
            .ReturnsAsync((ServerBanDef?) null);

        var resolved = await BanManager.ResolveServerBanForWebhook(db.Object, ban, CreateSawmill());

        Assert.That(resolved, Is.SameAs(ban));
    }

    [Test]
    public async Task FailedLookupFallsBackToOriginalBan()
    {
        var userId = new NetUserId(Guid.NewGuid());
        var ban = CreateBan(userId: userId);
        var db = new Mock<IServerDbManager>();
        db.Setup(d => d.GetServerBanAsync(null, userId, null, null))
            .ThrowsAsync(new InvalidOperationException("lookup failed"));

        var resolved = await BanManager.ResolveServerBanForWebhook(db.Object, ban, CreateSawmill());

        Assert.That(resolved, Is.SameAs(ban));
    }

    private static ServerBanDef CreateBan(
        int? id = null,
        NetUserId? userId = null,
        (IPAddress, int)? address = null,
        ImmutableTypedHwid? hwid = null)
    {
        return new ServerBanDef(
            id,
            userId,
            address,
            hwid,
            DateTimeOffset.UtcNow,
            null,
            null,
            TimeSpan.Zero,
            "test ban",
            NoteSeverity.Medium,
            null,
            null);
    }

    private static ISawmill CreateSawmill()
    {
        return new LogManager().GetSawmill("ban-webhook-test");
    }

    private static bool Matches(ImmutableArray<byte>? actual, ImmutableArray<byte> expected)
    {
        return actual is { Length: > 0 } value && value.SequenceEqual(expected);
    }

    private static bool MatchesModern(
        ImmutableArray<ImmutableArray<byte>>? actual,
        ImmutableArray<byte> expected)
    {
        return actual is { Length: 1 } value && value[0].SequenceEqual(expected);
    }
}
