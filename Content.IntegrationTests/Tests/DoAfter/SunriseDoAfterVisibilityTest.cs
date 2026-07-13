using Content.Shared._Sunrise.DoAfter.Components;
using Content.Shared.Stealth.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.DoAfter;

[TestFixture]
public sealed class SunriseDoAfterVisibilityTest
{
    [Test]
    public async Task StealthComponentAddsAndRemovesDoAfterMarker()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        EntityUid entity = default;

        await server.WaitPost(() =>
        {
            entity = server.EntMan.SpawnEntity(null, MapCoordinates.Nullspace);
            server.EntMan.EnsureComponent<StealthComponent>(entity);
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.HasComponent<SunriseHideDoAfterComponent>(entity), Is.True);
        });

        await server.WaitPost(() => server.EntMan.RemoveComponent<StealthComponent>(entity));

        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.HasComponent<SunriseHideDoAfterComponent>(entity), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
