using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Content.IntegrationTests.Pair;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.NetTextures;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
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
    public async Task InvalidSavedLobbyArtUsesTransientFallbackWithoutMutatingSettings()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var stateManager = client.Resolve<IStateManager>();
        var protoMan = client.Resolve<IPrototypeManager>();
        var entityManager = client.Resolve<IEntityManager>();

        const string fallbackArtId = "NetTexturesRegressionFallbackArt";
        const string invalidArtId = "NetTexturesRegressionMissingArt";
        const string artPrototype = """
- type: lobbyArt
  id: NetTexturesRegressionFallbackArt
  background: Logo/logo.png
""";

        await client.WaitPost(() =>
        {
            protoMan.LoadString(artPrototype, overwrite: true);
            protoMan.ResolveResults();
            SetLobbyTickerFallbacks(entityManager.System<ClientGameTicker>(), lobbyArt: fallbackArtId);
            client.CfgMan.SetCVar(SunriseCCVars.LobbyArt, invalidArtId);
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
            Assert.That(client.CfgMan.GetCVar(SunriseCCVars.LobbyArt), Is.EqualTo(invalidArtId));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SavedUnavailableWhitelistedLobbyArtUsesTransientAllowedFallbackWithoutMutatingSettings()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var stateManager = client.Resolve<IStateManager>();
        var resourceCache = client.Resolve<IResourceCache>();
        var clientProtoMan = client.Resolve<IPrototypeManager>();
        var serverProtoMan = pair.Server.Resolve<IPrototypeManager>();

        const string presetId = "NetTexturesRegressionWhitelistArtOnly";
        const string allowedArtId = "NetTexturesRegressionAllowedWhitelistArt";
        const string deniedArtId = "NetTexturesRegressionDeniedWhitelistArt";
        const string allowedArtPath = "Logo/logo.png";
        const string deniedArtPath = "Interface/VerbIcons/delete_transparent.svg.192dpi.png";
        var whitelistPrototype = $$"""
- type: lobbyArt
  id: {{allowedArtId}}
  background: {{allowedArtPath}}
- type: lobbyArt
  id: {{deniedArtId}}
  background: {{deniedArtPath}}
- type: lobbyBackgroundPreset
  id: {{presetId}}
  whitelistArts:
  - {{allowedArtId}}
""";

        await pair.Server.WaitPost(() =>
        {
            serverProtoMan.LoadString(whitelistPrototype, overwrite: true);
            serverProtoMan.ResolveResults();
        });
        await client.WaitPost(() =>
        {
            clientProtoMan.LoadString(whitelistPrototype, overwrite: true);
            clientProtoMan.ResolveResults();
            client.CfgMan.SetCVar(SunriseCCVars.LobbyArt, deniedArtId);
            client.CfgMan.SetCVar(SunriseCCVars.LobbyBackgroundType, "Art");
        });

        pair.Server.CfgMan.SetCVar(SunriseCCVars.LobbyBackgroundPreset, presetId);
        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var lobbyState = stateManager.CurrentState as LobbyState;
            Assert.That(resourceCache.TryGetResource<TextureResource>(new ResPath("/Textures/Logo/logo.png"), out var allowedTextureResource), Is.True);
            Assert.That(resourceCache.TryGetResource<TextureResource>(new ResPath("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png"), out var deniedTextureResource), Is.True);
            Assert.That(lobbyState, Is.Not.Null);
            Assert.That(lobbyState!.Lobby, Is.Not.Null);
            Assert.That(lobbyState.Lobby!.LoadingAnimationContainer.Visible, Is.False);
            Assert.That(lobbyState.Lobby.LobbyArt.Visible, Is.True);
            Assert.That(lobbyState.Lobby.LobbyArt.Texture, Is.EqualTo(allowedTextureResource!.Texture));
            Assert.That(lobbyState.Lobby.LobbyArt.Texture, Is.Not.EqualTo(deniedTextureResource!.Texture));
            Assert.That(client.CfgMan.GetCVar(SunriseCCVars.LobbyArt), Is.EqualTo(deniedArtId));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task InvalidSavedNetworkLobbyAnimationUsesTransientFallbackWithoutMutatingSettings()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var manager = client.ResolveDependency<ClientNetTexturesManager>();
        var stateManager = client.Resolve<IStateManager>();
        var protoMan = client.Resolve<IPrototypeManager>();
        var entityManager = client.Resolve<IEntityManager>();

        const string fallbackAnimationId = "NetTexturesRegressionFallbackAnimation";
        const string invalidAnimationId = "NetTexturesRegressionMissingAnimation";
        const string animationResourcePath = "/NetTextures/Test/invalid-saved-animation.rsi";
        const string animationStateId = "idle";
        const string animationPrototype = """
- type: lobbyAnimation
  id: NetTexturesRegressionFallbackAnimation
  animation: /NetTextures/Test/invalid-saved-animation.rsi
  state: idle
""";

        await client.WaitPost(() =>
        {
            protoMan.LoadString(animationPrototype, overwrite: true);
            protoMan.ResolveResults();
            SetLobbyTickerFallbacks(entityManager.System<ClientGameTicker>(), lobbyAnimation: fallbackAnimationId);
            manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (new ResPath($"{animationResourcePath}/meta.json").ToRelativePath(), CreateRsiMetaJson([animationStateId])),
                (new ResPath($"{animationResourcePath}/{animationStateId}.png").ToRelativePath(), CreatePngBytes(16, 16, seed: 61))
            });
            client.CfgMan.SetCVar(SunriseCCVars.LobbyAnimation, invalidAnimationId);
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
            Assert.That(client.CfgMan.GetCVar(SunriseCCVars.LobbyAnimation), Is.EqualTo(invalidAnimationId));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AnimationOnlyWhitelistFallsBackFromSavedArtTypeWithoutBlackScreen()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            InLobby = true,
            Dirty = true
        });

        var client = pair.Client;
        var stateManager = client.Resolve<IStateManager>();
        var clientGameTicker = client.Resolve<IEntityManager>().System<ClientGameTicker>();
        var clientProtoMan = client.Resolve<IPrototypeManager>();
        var serverProtoMan = pair.Server.Resolve<IPrototypeManager>();

        const string presetId = "NetTexturesRegressionWhitelistAnimationOnly";
        const string animationId = "NetTexturesRegressionWhitelistedAnimation";
        const string deniedArtId = "NetTexturesRegressionUnusedArt";
        var whitelistPrototype = $$"""
- type: lobbyAnimation
  id: {{animationId}}
  animation: _Sunrise/loading.rsi
  state: loading
- type: lobbyArt
  id: {{deniedArtId}}
  background: Logo/logo.png
- type: lobbyBackgroundPreset
  id: {{presetId}}
  whitelistAnimations:
  - {{animationId}}
""";

        await pair.Server.WaitPost(() =>
        {
            serverProtoMan.LoadString(whitelistPrototype, overwrite: true);
            serverProtoMan.ResolveResults();
        });
        await client.WaitPost(() =>
        {
            clientProtoMan.LoadString(whitelistPrototype, overwrite: true);
            clientProtoMan.ResolveResults();
            client.CfgMan.SetCVar(SunriseCCVars.LobbyArt, deniedArtId);
            client.CfgMan.SetCVar(SunriseCCVars.LobbyBackgroundType, "Art");
        });

        pair.Server.CfgMan.SetCVar(SunriseCCVars.LobbyBackgroundPreset, presetId);
        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var lobbyState = stateManager.CurrentState as LobbyState;
            Assert.That(lobbyState, Is.Not.Null);
            Assert.That(lobbyState!.Lobby, Is.Not.Null);
            Assert.That(clientGameTicker.LobbyType, Is.EqualTo("Animation"));
            Assert.That(clientGameTicker.LobbyAnimation?.ToString(), Is.EqualTo(animationId));
            Assert.That(lobbyState.Lobby!.LoadingAnimationContainer.Visible, Is.False);
            Assert.That(lobbyState.Lobby.LobbyAnimation.Visible, Is.True);
            Assert.That(lobbyState.Lobby.LobbyAnimation.DisplayRect.Texture, Is.Not.Null);
            Assert.That(lobbyState.Lobby.LobbyArt.Visible, Is.False);
            Assert.That(client.CfgMan.GetCVar(SunriseCCVars.LobbyBackgroundType), Is.EqualTo("Art"));
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
    public async Task CorruptRsiMetadataMarksPendingResourceFailed()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true
        });

        var client = pair.Client;
        var manager = client.ResolveDependency<ClientNetTexturesManager>();
        const string resourcePath = "/NetTextures/Test/corrupt-meta.rsi";
        var previousFailureLogLevel = LogLevel.Warning;

        await client.WaitPost(() => previousFailureLogLevel = client.CfgMan.GetCVar(RTCVars.FailureLogLevel));
        try
        {
            await client.WaitPost(() => client.CfgMan.SetCVar(RTCVars.FailureLogLevel, LogLevel.Error));
            await client.WaitPost(() => Assert.That(manager.EnsureResource(resourcePath), Is.False));

            await client.WaitPost(() => manager.PublishFiles(new List<(ResPath Relative, byte[] Data)>
            {
                (new ResPath($"{resourcePath}/meta.json").ToRelativePath(), Encoding.UTF8.GetBytes("{"))
            }));

            await client.WaitAssertion(() =>
            {
                Assert.That(GetPrivateField<HashSet<string>>(manager, "_failedResources"), Contains.Item(resourcePath));
                Assert.That(GetPrivateField<Dictionary<string, ResPath>>(manager, "_pendingResources").ContainsKey(resourcePath), Is.False);
                Assert.That(manager.TryGetAnimationState(resourcePath, "idle", out _), Is.False);
            });

            await client.WaitPost(() => Assert.That(manager.EnsureResource(resourcePath), Is.False));
        }
        finally
        {
            await client.WaitPost(() => client.CfgMan.SetCVar(RTCVars.FailureLogLevel, previousFailureLogLevel));
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
        var chunks = ServerNetTexturesManager.CreateFallbackChunks(relativePath, payload, NetTextureConstants.MaxChunkSize).ToArray();
        var loadedCount = 0;
        void Handler(string path)
        {
            if (path == resourcePath)
                Interlocked.Increment(ref loadedCount);
        }

        Assert.That(payload.Length, Is.GreaterThan(NetTextureConstants.MaxChunkSize));
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

    [Test]
    public async Task LateTransferBatchFromPreviousSessionIsIgnoredAfterLobbyReconnect()
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

        const string resourcePath = "/NetTextures/Test/late-transfer-reconnect.png";
        var relativePath = new ResPath(resourcePath).ToRelativePath();
        var uploadedPath = manager.GetUploadedPath(resourcePath);
        var png = CreatePngBytes(32, 32, seed: 71, noisy: true);
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
            var previousGeneration = 0;
            await client.WaitPost(() => previousGeneration = GetPrivateField<int>(manager, "_sessionGeneration"));

            var username = playerManager.Sessions.Single().Name;
            await DisconnectAndReconnectToLobby(pair, netManager, username);

            await client.WaitPost(() => InvokePrivateMethod(
                manager,
                "ReceiveNetTexturesTransferWorker",
                CreateTransferStream([(relativePath, png)]),
                previousGeneration));

            await pair.RunTicksSync(10);

            await client.WaitAssertion(() =>
            {
                Assert.That(stateManager.CurrentState, Is.TypeOf<LobbyState>());
                Assert.That(resources.ContentFileExists(uploadedPath), Is.False);
                Assert.That(manager.TryGetTexture(resourcePath, out _), Is.False);
            });
            Assert.That(Volatile.Read(ref loadedCount), Is.Zero);

            var currentGeneration = 0;
            await client.WaitPost(() => currentGeneration = GetPrivateField<int>(manager, "_sessionGeneration"));
            Assert.That(currentGeneration, Is.Not.EqualTo(previousGeneration));

            await client.WaitPost(() => InvokePrivateMethod(
                manager,
                "ReceiveNetTexturesTransferWorker",
                CreateTransferStream([(relativePath, png)]),
                currentGeneration));

            await client.WaitAssertion(() => Assert.That(resources.ContentFileExists(uploadedPath), Is.True));
            await client.WaitPost(() => _ = manager.EnsureResource(resourcePath));
            await WaitUntilTextureReady(client, manager, resourcePath, maxTicks: 120);

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

    private static MemoryStream CreateTransferStream(List<(ResPath Relative, byte[] Data)> files)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            for (var i = 0; i < files.Count; i++)
            {
                var (relative, data) = files[i];
                var pathBytes = Encoding.UTF8.GetBytes(relative.ToString());

                writer.Write((uint) pathBytes.Length);
                writer.Write((uint) data.Length);
                writer.Write(pathBytes);
                writer.Write(data);
                writer.Write(i == files.Count - 1 ? (byte) 0 : (byte) 1);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static void InvokePrivateMethod(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Failed to find private method {methodName}.");

        try
        {
            method!.Invoke(instance, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
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

    private static void SetLobbyTickerFallbacks(
        ClientGameTicker gameTicker,
        string? lobbyType = null,
        string? lobbyParallax = null,
        string? lobbyAnimation = null,
        string? lobbyArt = null)
    {
        gameTicker.SetTestFallbacks(lobbyType, lobbyParallax, lobbyAnimation, lobbyArt);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Failed to find private field {fieldName}.");
        return (T) field!.GetValue(instance)!;
    }
}
