using System;
using Content.Server.Administration.Managers;
using NUnit.Framework;

namespace Content.Tests.Server._Sunrise.Administration;

[TestFixture]
public sealed class StellarAdminRbacChangedPayloadTests
{
    [Test]
    public void TryValidateStellarAdminRbacChangedPayload_RejectsMissingProject()
    {
        Assert.That(
            AdminManager.TryValidateStellarAdminRbacChangedPayload(
                null,
                "550e8400-e29b-41d4-a716-446655440000",
                "rev",
                out _,
                out _),
            Is.False);

        Assert.That(
            AdminManager.TryValidateStellarAdminRbacChangedPayload(
                "   ",
                "550e8400-e29b-41d4-a716-446655440000",
                "rev",
                out _,
                out _),
            Is.False);
    }

    [Test]
    public void TryValidateStellarAdminRbacChangedPayload_RejectsMissingRevision()
    {
        Assert.That(
            AdminManager.TryValidateStellarAdminRbacChangedPayload(
                "test-sunrise-station",
                "550e8400-e29b-41d4-a716-446655440000",
                null,
                out _,
                out _),
            Is.False);

        Assert.That(
            AdminManager.TryValidateStellarAdminRbacChangedPayload(
                "test-sunrise-station",
                "550e8400-e29b-41d4-a716-446655440000",
                "   ",
                out _,
                out _),
            Is.False);
    }

    [Test]
    public void TryValidateStellarAdminRbacChangedPayload_RejectsInvalidUserId()
    {
        Assert.That(
            AdminManager.TryValidateStellarAdminRbacChangedPayload(
                "test-sunrise-station",
                "not-a-guid",
                "rev",
                out _,
                out _),
            Is.False);
    }

    [Test]
    public void TryValidateStellarAdminRbacChangedPayload_TrimsProjectAndParsesUserId()
    {
        var result = AdminManager.TryValidateStellarAdminRbacChangedPayload(
            "  test-sunrise-station  ",
            "  550e8400-e29b-41d4-a716-446655440000  ",
            "  rev  ",
            out var projectSlug,
            out var userGuid);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(projectSlug, Is.EqualTo("test-sunrise-station"));
            Assert.That(userGuid, Is.EqualTo(Guid.Parse("550e8400-e29b-41d4-a716-446655440000")));
        });
    }
}
