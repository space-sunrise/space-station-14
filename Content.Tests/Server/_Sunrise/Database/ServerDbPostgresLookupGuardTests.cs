using System.Collections.Immutable;
using System.Net;
using Content.Server.Database;
using NUnit.Framework;

namespace Content.Tests.Server._Sunrise.Database;

[TestFixture]
public sealed class ServerDbPostgresLookupGuardTests
{
    [Test]
    public void ModernHwidOnlyLookupHasLookupKey()
    {
        var modernHwIds = ImmutableArray.Create(ImmutableArray.Create<byte>(1, 2, 3));

        Assert.That(ServerDbPostgres.HasBanLookupKey(null, null, null, modernHwIds), Is.True);
    }

    [Test]
    public void EmptyLookupHasNoLookupKey()
    {
        Assert.That(ServerDbPostgres.HasBanLookupKey(null, null, null, null), Is.False);
        Assert.That(ServerDbPostgres.HasBanLookupKey(null, null, [], []), Is.False);
    }

    [Test]
    public void AddressOnlyLookupHasLookupKey()
    {
        Assert.That(
            ServerDbPostgres.HasBanLookupKey(IPAddress.Parse("203.0.113.10"), null, null, null),
            Is.True);
    }
}
