// Sunrise-Edit
using System.Net;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Sunrise.Interfaces.Shared;
using Content.Server.Connection;
using Content.Sunrise.Interfaces.Server;
using Moq;
using NUnit.Framework;
using Robust.Client;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Client.Console;

namespace Content.IntegrationTests.Tests._Sunrise.Sponsors
{
    [TestFixture]
    public sealed class SponsorPriorityJoinTest
    {
        [Test]
        public async Task TestPriorityJoinBypassSoftCap()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var server = pair.Server;
            var client = pair.Client;

            var cfg = server.ResolveDependency<IConfigurationManager>();

            // Save original settings
            var originalSoftMaxPlayers = cfg.GetCVar(CCVars.SoftMaxPlayers);
            var originalQueueEnabled = cfg.GetCVar(SunriseCCVars.QueueEnabled);
            object? originalSponsorsMgr = null;
            var connMgr = server.ResolveDependency<IConnectionManager>();
            var field = typeof(ConnectionManager).GetField("_sponsorsMgr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                originalSponsorsMgr = field.GetValue(connMgr);
            }

            try
            {
                // 1. Setup: Server is "full" and queue is disabled
                await server.WaitPost(() => {
                    cfg.SetCVar(CCVars.SoftMaxPlayers, 0);
                    cfg.SetCVar(SunriseCCVars.QueueEnabled, false);
                });

                // 2. Setup: Mock sponsor manager that grants priority join
                var sponsorMock = new Mock<ISharedSponsorsManager>();
                sponsorMock.Setup(m => m.HavePriorityJoin(It.IsAny<NetUserId>())).Returns(true);

                await server.WaitPost(() => {
                    field!.SetValue(connMgr, sponsorMock.Object);
                });

                // 3. Action: Disconnect and reconnect
                var clientNetManager = client.ResolveDependency<IClientNetManager>();
                var clientConsole = client.ResolveDependency<IClientConsoleHost>();
                var baseClient = client.ResolveDependency<IBaseClient>();

                await client.WaitPost(() => clientConsole.ExecuteCommand("disconnect"));
                await pair.RunTicksSync(10);

                client.SetConnectTarget(server);
                // Use IBaseClient to ensure correct state machine transitions (RunLevel)
                await client.WaitPost(() => baseClient.ConnectToServer(new DnsEndPoint("localhost", 1212)));

                // 4. Verification: Should connect successfully despite the soft cap
                await pair.RunTicksSync(20);

                Assert.That(clientNetManager.IsConnected, "Client should be connected despite soft cap because of priority join.");
            }
            finally
            {
                // Restore settings
                await server.WaitPost(() => {
                    cfg.SetCVar(CCVars.SoftMaxPlayers, originalSoftMaxPlayers);
                    cfg.SetCVar(SunriseCCVars.QueueEnabled, originalQueueEnabled);
                    if (field != null)
                    {
                        field.SetValue(connMgr, originalSponsorsMgr);
                    }
                });

                // Reconnect client in case it got disconnected
                var clientNetManager = client.ResolveDependency<IClientNetManager>();
                var baseClient = client.ResolveDependency<IBaseClient>();
                bool isConnected = false;
                await client.WaitPost(() => isConnected = clientNetManager.IsConnected);
                if (!isConnected)
                {
                    client.SetConnectTarget(server);
                    await client.WaitPost(() => baseClient.ConnectToServer(new DnsEndPoint("localhost", 1212)));
                    await pair.RunTicksSync(20);
                }

                await pair.CleanReturnAsync();
            }
        }

        [Test]
        public async Task TestNonSponsorBlockedBySoftCap()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var server = pair.Server;
            var client = pair.Client;

            var cfg = server.ResolveDependency<IConfigurationManager>();

            // Save original settings
            var originalSoftMaxPlayers = cfg.GetCVar(CCVars.SoftMaxPlayers);
            var originalQueueEnabled = cfg.GetCVar(SunriseCCVars.QueueEnabled);
            object? originalSponsorsMgr = null;
            var connMgr = server.ResolveDependency<IConnectionManager>();
            var field = typeof(ConnectionManager).GetField("_sponsorsMgr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                originalSponsorsMgr = field.GetValue(connMgr);
            }

            try
            {
                // 1. Setup: Server is "full" and queue is disabled
                await server.WaitPost(() => {
                    cfg.SetCVar(CCVars.SoftMaxPlayers, 0);
                    cfg.SetCVar(SunriseCCVars.QueueEnabled, false);
                });

                // 2. Setup: Mock sponsor manager that DENIES priority join
                var sponsorMock = new Mock<ISharedSponsorsManager>();
                sponsorMock.Setup(m => m.HavePriorityJoin(It.IsAny<NetUserId>())).Returns(false);

                await server.WaitPost(() => {
                    field!.SetValue(connMgr, sponsorMock.Object);
                });

                // 3. Action: Disconnect and reconnect
                var clientNetManager = client.ResolveDependency<IClientNetManager>();
                var clientConsole = client.ResolveDependency<IClientConsoleHost>();
                var baseClient = client.ResolveDependency<IBaseClient>();

                await client.WaitPost(() => clientConsole.ExecuteCommand("disconnect"));
                await pair.RunTicksSync(10);

                client.SetConnectTarget(server);
                await client.WaitPost(() => baseClient.ConnectToServer(new DnsEndPoint("localhost", 1212)));

                // 4. Verification: Should NOT connect successfully
                await pair.RunTicksSync(20);

                Assert.That(!clientNetManager.IsConnected, "Client should NOT be connected because server is full and they have no priority join.");
            }
            finally
            {
                // Restore settings
                await server.WaitPost(() => {
                    cfg.SetCVar(CCVars.SoftMaxPlayers, originalSoftMaxPlayers);
                    cfg.SetCVar(SunriseCCVars.QueueEnabled, originalQueueEnabled);
                    if (field != null)
                    {
                        field.SetValue(connMgr, originalSponsorsMgr);
                    }
                });

                // Reconnect client so the pair is returned to the pool in a fully connected state
                var clientNetManager = client.ResolveDependency<IClientNetManager>();
                var baseClient = client.ResolveDependency<IBaseClient>();
                bool isConnected = false;
                await client.WaitPost(() => isConnected = clientNetManager.IsConnected);
                if (!isConnected)
                {
                    client.SetConnectTarget(server);
                    await client.WaitPost(() => baseClient.ConnectToServer(new DnsEndPoint("localhost", 1212)));
                    await pair.RunTicksSync(20);
                }

                await pair.CleanReturnAsync();
            }
        }
    }
}
