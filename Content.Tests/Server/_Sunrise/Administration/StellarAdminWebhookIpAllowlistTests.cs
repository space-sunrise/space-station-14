using System.Net;
using Content.Server.Administration.Managers;
using NUnit.Framework;

namespace Content.Tests.Server._Sunrise.Administration;

[TestFixture]
public sealed class StellarAdminWebhookIpAllowlistTests
{
    [Test]
    public void AllowlistDisabledDoesNotBlockWebhookByIp()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: false,
            allowlist: string.Empty,
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("198.51.100.10"),
            xForwardedFor: null,
            forwarded: null,
            out var resolvedClientIp);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(resolvedClientIp, Is.EqualTo(IPAddress.Parse("198.51.100.10")));
        });
    }

    [Test]
    public void AllowlistEnabledAllowsExactIpMatch()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: "203.0.113.10",
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("203.0.113.10"),
            xForwardedFor: null,
            forwarded: null,
            out var resolvedClientIp);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(resolvedClientIp, Is.EqualTo(IPAddress.Parse("203.0.113.10")));
        });
    }

    [Test]
    public void AllowlistEnabledAllowsCidrMatch()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: "198.51.100.0/24",
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("198.51.100.42"),
            xForwardedFor: null,
            forwarded: null,
            out _);

        Assert.That(result, Is.True);
    }

    [Test]
    public void AllowlistEnabledRejectsDeniedIp()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: "203.0.113.10",
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("198.51.100.10"),
            xForwardedFor: null,
            forwarded: null,
            out _);

        Assert.That(result, Is.False);
    }

    [TestCase("")]
    [TestCase("not-an-ip")]
    [TestCase("203.0.113.10/99")]
    public void EnabledEmptyOrInvalidAllowlistFailsClosed(string allowlist)
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: allowlist,
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("203.0.113.10"),
            xForwardedFor: null,
            forwarded: null,
            out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void DirectBareMetalRequestUsesRemoteEndPoint()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: "198.51.100.10",
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("198.51.100.10"),
            xForwardedFor: "203.0.113.10",
            forwarded: null,
            out var resolvedClientIp);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(resolvedClientIp, Is.EqualTo(IPAddress.Parse("198.51.100.10")));
        });
    }

    [Test]
    public void SpoofedXForwardedForIsIgnoredWithoutTrustedProxy()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: "203.0.113.10",
            trustedProxyCidrs: string.Empty,
            remoteIp: IPAddress.Parse("198.51.100.10"),
            xForwardedFor: "203.0.113.10",
            forwarded: null,
            out var resolvedClientIp);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(resolvedClientIp, Is.EqualTo(IPAddress.Parse("198.51.100.10")));
        });
    }

    [Test]
    public void TrustedProxyXForwardedForUsesForwardedClientIp()
    {
        var result = AdminManager.IsStellarAdminWebhookClientIpAllowed(
            allowlistEnabled: true,
            allowlist: "203.0.113.10",
            trustedProxyCidrs: "127.0.0.1/32",
            remoteIp: IPAddress.Parse("127.0.0.1"),
            xForwardedFor: "203.0.113.10",
            forwarded: null,
            out var resolvedClientIp);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(resolvedClientIp, Is.EqualTo(IPAddress.Parse("203.0.113.10")));
        });
    }
}
