using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Content.Client._Sunrise;
using Content.Client.Resources;
using Content.Client.IoC;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Data;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ImageParallaxTextureSource : IParallaxTextureSource
{
    /// <summary>
    /// Texture path.
    /// </summary>
    [DataField("path", required: true)]
    public ResPath Path { get; private set; } = default!;

    async Task<Texture> IParallaxTextureSource.GenerateTexture(CancellationToken cancel)
    {
        // Check if this is a network texture (starts with /NetTextures/)
        var pathStr = Path.ToString();
        if (pathStr.StartsWith("/NetTextures/", System.StringComparison.Ordinal))
        {
            // Use NetTexturesManager for dynamic loading
            var netTexturesManager = IoCManager.Resolve<NetTexturesManager>();
            var resourceManager = IoCManager.Resolve<IResourceManager>();
            var netManager = IoCManager.Resolve<IClientNetManager>();

            // If client is not connected to server, fallback to local resources
            if (!netManager.IsConnected)
            {
                // Try to load from local resources as fallback
                // Convert /NetTextures/Parallaxes/... to /Textures/Parallaxes/... for local fallback
                var fallbackPath = pathStr.Replace("/NetTextures/", "/Textures/");
                var fallbackResPath = new ResPath(fallbackPath);
                if (resourceManager.ContentFileExists(fallbackResPath))
                {
                    return StaticIoC.ResC.GetTexture(fallbackResPath);
                }
                // If fallback path doesn't exist, try to use a default texture
                // This ensures we always have something to display before connecting
                var defaultPath = new ResPath("/Textures/Parallaxes/layer1.png");
                if (resourceManager.ContentFileExists(defaultPath))
                {
                    return StaticIoC.ResC.GetTexture(defaultPath);
                }
                // Last resort: try original path (might fail, but at least we tried)
                return StaticIoC.ResC.GetTexture(Path);
            }

            // Ensure the resource is available
            var isAvailable = netTexturesManager.EnsureResource(pathStr);

            if (!isAvailable)
            {
                // Resource is being requested, wait for it to load
                var tcs = new TaskCompletionSource<bool>();
                void OnResourceLoaded(string loadedPath)
                {
                    if (loadedPath == pathStr)
                    {
                        netTexturesManager.ResourceLoaded -= OnResourceLoaded;
                        tcs.TrySetResult(true);
                    }
                }

                netTexturesManager.ResourceLoaded += OnResourceLoaded;

                try
                {
                    if (netTexturesManager.TryGetTexture(pathStr, out var readyTexture))
                    {
                        netTexturesManager.ResourceLoaded -= OnResourceLoaded;
                        if (readyTexture != null)
                            return readyTexture;
                    }
                    else
                    {
                        // Wait for the resource to load (with cancellation support)
                        using (cancel.Register(() =>
                        {
                            netTexturesManager.ResourceLoaded -= OnResourceLoaded;
                            tcs.TrySetCanceled();
                        }))
                        {
                            await tcs.Task;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Cancellation requested, fallback to original path
                    return StaticIoC.ResC.GetTexture(Path);
                }
            }

            if (netTexturesManager.TryGetTexture(pathStr, out var texture))
            {
                if (texture != null)
                    return texture;
            }

            // Fallback to original path if network texture loading failed
            return StaticIoC.ResC.GetTexture(Path);
        }
        
        // For non-network textures, use the original method
        return StaticIoC.ResC.GetTexture(Path);
    }
}

