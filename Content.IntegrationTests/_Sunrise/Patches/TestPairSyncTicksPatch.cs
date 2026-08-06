#nullable enable
using System.Reflection;
using MonoMod.RuntimeDetour;
using Robust.UnitTesting;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests._Sunrise.Patches;

using EngineTestPair = Robust.UnitTesting.Pool.TestPair<
    RobustIntegrationTest.ServerIntegrationInstance,
    RobustIntegrationTest.ClientIntegrationInstance>;

/// <summary>
/// Повторяет коррекцию тиков тестовой пары, если одна попытка не успела изменить разницу тиков.
/// </summary>
internal static class TestPairSyncTicksPatch
{
    private const int MaximumAttempts = 10;

    private static Hook? _hook;

    private delegate Task SyncTicksDelegate(EngineTestPair pair, int targetDelta);

    internal static void Apply()
    {
        if (_hook != null)
            return;

        var method = typeof(EngineTestPair).GetMethod(
            nameof(ITestPair.SyncTicks),
            BindingFlags.Instance | BindingFlags.Public);

        if (method == null)
        {
            TestContext.Error.WriteLine(
                "[TestPairSyncTicksPatch] SyncTicks method was not found; tick synchronization retries are disabled.");
            return;
        }

        _hook = new Hook(method, SyncTicksReplacement);
    }

    internal static void Unpatch()
    {
        _hook?.Dispose();
        _hook = null;
    }

    private static async Task SyncTicksReplacement(
        SyncTicksDelegate _,
        EngineTestPair pair,
        int targetDelta)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var serverTick = (int) pair.Server.Timing.CurTick.Value;
            var clientTick = (int) pair.Client.Timing.CurTick.Value;
            var delta = clientTick - serverTick;

            if (delta == targetDelta)
                return;

            if (delta > targetDelta)
                await pair.Server.WaitRunTicks(delta - targetDelta);
            else
                await pair.Client.WaitRunTicks(targetDelta - delta);
        }

        var finalServerTick = (int) pair.Server.Timing.CurTick.Value;
        var finalClientTick = (int) pair.Client.Timing.CurTick.Value;
        var finalDelta = finalClientTick - finalServerTick;
        Assert.That(finalDelta, Is.EqualTo(targetDelta),
            $"Failed to synchronize client and server ticks after {MaximumAttempts} attempts.");
    }
}
