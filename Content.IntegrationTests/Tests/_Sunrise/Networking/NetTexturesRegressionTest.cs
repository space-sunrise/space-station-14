using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Content.IntegrationTests.Pair;
using Content.Client.Lobby;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.NetTextures;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using ClientNetTexturesManager = Content.Client._Sunrise.NetTexturesManager;
using ServerNetTexturesManager = Content.Server._Sunrise.NetTexturesManager;

namespace Content.IntegrationTests.Tests._Sunrise.Networking;

[TestFixture]
[TestOf(typeof(ClientNetTexturesManager))]
public sealed class NetTexturesRegressionTest
{
    private const int FallbackChunkSize = 64 * 1024;

    [Test]
    public async Task LocalLobbyArtLoadsWithoutNetTextures()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var stateManager = client.Resolve<IStateManager>();
        var protoMan = client.Resolve<IPrototypeManager>();

        const string artId = "NetTexturesRegressionLocalArt";
        const string artPrototype = """
- type: lobbyArt
  id: NetTexturesRegressionLocalArt
  background: Logo/logo.png
""";

        await client.WaitPost(() =>
        {
            protoMan.LoadString(artPrototype, overwrite: true);
            protoMan.ResolveResults();
            client.CfgMan.SetCVar(SunriseCCVars.LobbyArt, artId);
            client.CfgMan.SetCVar(SunriseCCVars.LobbyBackgroundType, "Art");
        });

        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            var lobbyState = stateManager.CurrentState as LobbyState;
            Assert.That(lobbyState, Is.Not.Null);
            Assert.That(lobbyState!.Lobby, Is.Not.Null);
            Assert.That(lobbyState.Lobby!.LoadingAnimationContainer.Visible, Is.False);
            Assert.That(lobbyState.Lobby.LobbyArt.Visible, Is.True);
            Assert.That(lobbyState.Lobby.LobbyArt.Texture, Is.Not.Null);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LocalLobbyAnimationLoadsWithoutNetTextures()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var stateManager = client.Resolve<IStateManager>();
        var protoMan = client.Resolve<IPrototypeManager>();

        const string animationId = "NetTexturesRegressionLocalAnimation";
        const string animationPrototype = """
- type: lobbyAnimation
  id: NetTexturesRegressionLocalAnimation
  animation: _Sunrise/loading.rsi
  state: loading
""";

        await client.WaitPost(() =>
        {
            protoMan.LoadString(animationPrototype, overwrite: true);
            protoMan.ResolveResults();
            client.CfgMan.SetCVar(SunriseCCVars.LobbyAnimation, animationId);
            client.CfgMan.SetCVar(SunriseCCVars.LobbyBackgroundType, "Animation");
        });

        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            var lobbyState = stateManager.CurrentState as LobbyState;
            Assert.That(lobbyState, Is.Not.Null);
            Assert.That(lobbyState!.Lobby, Is.Not.Null);
            Assert.That(lobbyState.Lobby!.LoadingAnimationContainer.Visible, Is.False);
            Assert.That(lobbyState.Lobby.LobbyAnimation.Visible, Is.True);
            Assert.That(lobbyState.Lobby.LobbyAnimation.DisplayRect.Texture, Is.Not.Null);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DisconnectFromLobbyClearsLoadedTextureAndAllowsReload()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var server = pair.Server;
        var manager = client.ResolveDependency<ClientNetTexturesManager>();
        var resources = client.ResolveDependency<IResourceManager>();
        var stateManager = client.Resolve<IStateManager>();
        var netManager = client.ResolveDependency<IClientNetManager>();
        var playerManager = server.ResolveDependency<Robust.Server.Player.IPlayerManager>();

        const string resourcePath = "/NetTextures/Test/reconnect.png";
        var relativePath = new ResPath(resourcePath).ToRelativePath();
        var uploadedPath = manager.GetUploadedPath(resourcePath);
        var png = CreatePngBytes(24, 24, seed: 11);
        var loadedCount = 0;
        void Handler(string path)
        {
            if (path == resourcePath)
                Interlocked.Increment(ref loadedCount);
        }

        await client.WaitAssertion(() =>
        {
            Assert.That(stateManager.CurrentState, Is.TypeOf<LobbyState>());
            manager.ResourceLoaded += Handler;
        });

        try
        {
            await client.WaitPost(() => manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (relativePath, png)
            }));
            await client.WaitAssertion(() => Assert.That(resources.ContentFileExists(uploadedPath), Is.True));
            await client.WaitPost(() => _ = manager.EnsureResource(resourcePath));
            await WaitUntilTextureReady(client, manager, resourcePath, maxTicks: 120);

            await client.WaitAssertion(() =>
            {
                Assert.That(manager.TryGetTexture(resourcePath, out var texture), Is.True);
                Assert.That(texture, Is.Not.Null);
                Assert.That(resources.ContentFileExists(uploadedPath), Is.True);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(1));

            await client.WaitPost(() => _ = manager.EnsureResource(resourcePath));
            await pair.RunTicksSync(5);
            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(1));

            var username = playerManager.Sessions.Single().Name;
            await DisconnectAndReconnectToLobby(pair, netManager, username);

            await client.WaitAssertion(() =>
            {
                Assert.That(stateManager.CurrentState, Is.TypeOf<LobbyState>());
                Assert.That(manager.TryGetTexture(resourcePath, out _), Is.False);
                Assert.That(resources.ContentFileExists(uploadedPath), Is.False);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(1));

            await client.WaitPost(() => manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (relativePath, png)
            }));
            await client.WaitPost(() => _ = manager.EnsureResource(resourcePath));
            await WaitUntilTextureReady(client, manager, resourcePath, maxTicks: 120);

            await client.WaitAssertion(() =>
            {
                Assert.That(manager.TryGetTexture(resourcePath, out var texture), Is.True);
                Assert.That(texture, Is.Not.Null);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(2));
        }
        finally
        {
            await client.WaitPost(() => manager.ResourceLoaded -= Handler);
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LargeStillTextureLoadsAcrossMultipleUploadTiles()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true
        });

        var client = pair.Client;
        var manager = client.ResolveDependency<ClientNetTexturesManager>();
        const string resourcePath = "/NetTextures/Test/large-art.png";
        var relativePath = new ResPath(resourcePath).ToRelativePath();
        var loadedCount = 0;

        void Handler(string path)
        {
            if (path == resourcePath)
                Interlocked.Increment(ref loadedCount);
        }

        await client.WaitPost(() => manager.ResourceLoaded += Handler);

        try
        {
            await client.WaitPost(() => manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (relativePath, CreatePngBytes(1400, 1300, seed: 51))
            }));

            await client.WaitAssertion(() => Assert.That(manager.EnsureResource(resourcePath), Is.False));
            await WaitUntilTextureReady(client, manager, resourcePath, maxTicks: 180);

            await client.WaitAssertion(() =>
            {
                Assert.That(manager.TryGetTexture(resourcePath, out var texture), Is.True);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture!.Width, Is.EqualTo(1400));
                Assert.That(texture.Height, Is.EqualTo(1300));
            });

            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(1));
        }
        finally
        {
            await client.WaitPost(() => manager.ResourceLoaded -= Handler);
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PartialRsiRequiresAllStateImagesBeforeReady()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true
        });

        var client = pair.Client;
        var manager = client.ResolveDependency<ClientNetTexturesManager>();
        const string resourcePath = "/NetTextures/Test/partial.rsi";
        var loadedCount = 0;
        void Handler(string path)
        {
            if (path == resourcePath)
                Interlocked.Increment(ref loadedCount);
        }

        await client.WaitPost(() => manager.ResourceLoaded += Handler);

        try
        {
            await client.WaitPost(() => manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (new ResPath("/NetTextures/Test/partial.rsi/meta.json").ToRelativePath(), CreateRsiMetaJson(["idle", "glow"])),
                (new ResPath("/NetTextures/Test/partial.rsi/idle.png").ToRelativePath(), CreatePngBytes(16, 16, seed: 21))
            }));

            await client.WaitAssertion(() => Assert.That(manager.EnsureResource(resourcePath), Is.False));
            await client.WaitRunTicks(5);

            await client.WaitAssertion(() =>
            {
                Assert.That(manager.TryGetAnimationState(resourcePath, "idle", out _), Is.False);
                Assert.That(manager.TryGetAnimationState(resourcePath, "glow", out _), Is.False);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.Zero);

            await client.WaitPost(() => manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (new ResPath("/NetTextures/Test/partial.rsi/glow.png").ToRelativePath(), CreatePngBytes(16, 16, seed: 22))
            }));
            await WaitUntilAnimationStateReady(client, manager, resourcePath, "glow", maxTicks: 120);

            await client.WaitAssertion(() =>
            {
                Assert.That(manager.TryGetAnimationState(resourcePath, "idle", out var idle), Is.True);
                Assert.That(idle, Is.Not.Null);
                Assert.That(manager.TryGetAnimationState(resourcePath, "glow", out var glow), Is.True);
                Assert.That(glow, Is.Not.Null);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(1));
        }
        finally
        {
            await client.WaitPost(() => manager.ResourceLoaded -= Handler);
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IncompleteFallbackAssemblyDoesNotSurviveLobbyReconnect()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var server = pair.Server;
        var manager = client.ResolveDependency<ClientNetTexturesManager>();
        var resources = client.ResolveDependency<IResourceManager>();
        var netManager = client.ResolveDependency<IClientNetManager>();
        var stateManager = client.Resolve<IStateManager>();
        var playerManager = server.ResolveDependency<Robust.Server.Player.IPlayerManager>();

        const string resourcePath = "/NetTextures/Test/fallback-large.png";
        var relativePath = new ResPath(resourcePath).ToRelativePath();
        var uploadedPath = manager.GetUploadedPath(resourcePath);
        var payload = CreatePngBytes(256, 256, seed: 44, noisy: true);
        var chunks = ServerNetTexturesManager.CreateFallbackChunks(relativePath, payload, FallbackChunkSize).ToArray();
        var loadedCount = 0;
        void Handler(string path)
        {
            if (path == resourcePath)
                Interlocked.Increment(ref loadedCount);
        }

        Assert.That(payload.Length, Is.GreaterThan(FallbackChunkSize));
        Assert.That(chunks.Length, Is.GreaterThan(1));

        await client.WaitAssertion(() =>
        {
            Assert.That(stateManager.CurrentState, Is.TypeOf<LobbyState>());
            manager.ResourceLoaded += Handler;
        });

        try
        {
            await DispatchFallbackChunk(client, manager, chunks[0]);
            await client.WaitAssertion(() => Assert.That(resources.ContentFileExists(uploadedPath), Is.False));

            var username = playerManager.Sessions.Single().Name;
            await DisconnectAndReconnectToLobby(pair, netManager, username);

            await DispatchFallbackChunk(client, manager, chunks[^1]);
            await client.WaitRunTicks(5);

            await client.WaitAssertion(() =>
            {
                Assert.That(stateManager.CurrentState, Is.TypeOf<LobbyState>());
                Assert.That(resources.ContentFileExists(uploadedPath), Is.False);
                Assert.That(manager.TryGetTexture(resourcePath, out _), Is.False);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.Zero);

            foreach (var chunk in chunks)
            {
                await DispatchFallbackChunk(client, manager, chunk);
            }

            await client.WaitAssertion(() => Assert.That(resources.ContentFileExists(uploadedPath), Is.True));
            await client.WaitPost(() => _ = manager.EnsureResource(resourcePath));
            await WaitUntilTextureReady(client, manager, resourcePath, maxTicks: 180);

            await client.WaitAssertion(() =>
            {
                Assert.That(manager.TryGetTexture(resourcePath, out var texture), Is.True);
                Assert.That(texture, Is.Not.Null);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.EqualTo(1));
        }
        finally
        {
            await client.WaitPost(() => manager.ResourceLoaded -= Handler);
        }

        await pair.CleanReturnAsync();
    }

    private static async Task DisconnectAndReconnectToLobby(
        TestPair pair,
        IClientNetManager netManager,
        string username)
    {
        await pair.Client.WaitPost(() => netManager.ClientDisconnect("NetTextures regression test"));
        await pair.RunTicksSync(10);
        await Task.WhenAll(pair.Client.WaitIdleAsync(), pair.Server.WaitIdleAsync());

        await pair.Client.WaitAssertion(() => Assert.That(netManager.IsConnected, Is.False));
        await pair.Server.WaitAssertion(() => Assert.That(pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>().PlayerCount, Is.EqualTo(0)));

        pair.Client.SetConnectTarget(pair.Server);
        await pair.Client.WaitPost(() => netManager.ClientConnect(null!, 0, username));
        await pair.RunTicksSync(10);
        await Task.WhenAll(pair.Client.WaitIdleAsync(), pair.Server.WaitIdleAsync());

        await pair.Client.WaitAssertion(() => Assert.That(netManager.IsConnected, Is.True));
        await pair.Server.WaitAssertion(() => Assert.That(pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>().PlayerCount, Is.EqualTo(1)));
        await pair.Client.WaitAssertion(() => Assert.That(pair.Client.Resolve<IStateManager>().CurrentState, Is.TypeOf<LobbyState>()));
    }

    private static async Task WaitUntilTextureReady(
        RobustIntegrationTest.ClientIntegrationInstance client,
        ClientNetTexturesManager manager,
        string resourcePath,
        int maxTicks)
    {
        await PoolManager.WaitUntil(client, async () =>
        {
            var ready = false;
            await client.WaitPost(() => ready = manager.TryGetTexture(resourcePath, out _));
            return ready;
        }, maxTicks: maxTicks);
    }

    private static async Task WaitUntilAnimationStateReady(
        RobustIntegrationTest.ClientIntegrationInstance client,
        ClientNetTexturesManager manager,
        string resourcePath,
        string stateId,
        int maxTicks)
    {
        await PoolManager.WaitUntil(client, async () =>
        {
            var ready = false;
            await client.WaitPost(() => ready = manager.TryGetAnimationState(resourcePath, stateId, out _));
            return ready;
        }, maxTicks: maxTicks);
    }

    private static async Task DispatchFallbackChunk(
        RobustIntegrationTest.ClientIntegrationInstance client,
        ClientNetTexturesManager manager,
        NetTextureResourceChunkMessage message)
    {
        await client.WaitPost(() => manager.ReceiveFallbackChunk(CloneMessage(message)));
    }

    private static NetTextureResourceChunkMessage CloneMessage(NetTextureResourceChunkMessage message)
    {
        return new NetTextureResourceChunkMessage
        {
            RelativePath = message.RelativePath,
            ChunkIndex = message.ChunkIndex,
            TotalChunks = message.TotalChunks,
            ChunkOffset = message.ChunkOffset,
            TotalLength = message.TotalLength,
            Data = message.Data.ToArray()
        };
    }

    private static byte[] CreateRsiMetaJson(string[] states)
    {
        var meta = new
        {
            size = new { x = 16, y = 16 },
            states = states.Select(state => new
            {
                name = state,
                directions = 1,
                delays = new[] { new[] { 1.0f } }
            }).ToArray()
        };

        return JsonSerializer.SerializeToUtf8Bytes(meta);
    }

    private static byte[] CreatePngBytes(int width, int height, int seed, bool noisy = false)
    {
        using var image = new Image<Rgba32>(width, height);
        var random = new Random(seed);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (noisy)
                {
                    image[x, y] = new Rgba32(
                        (byte) random.Next(256),
                        (byte) random.Next(256),
                        (byte) random.Next(256),
                        255);
                    continue;
                }

                image[x, y] = new Rgba32(
                    (byte) ((x * 31 + seed) % 256),
                    (byte) ((y * 17 + seed) % 256),
                    (byte) ((x * 13 + y * 7 + seed) % 256),
                    255);
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
