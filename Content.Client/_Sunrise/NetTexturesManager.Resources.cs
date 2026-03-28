using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Public API
    /// <summary>
    /// Ensures that a network resource has been requested and returns whether it is already ready for use.
    /// </summary>
    /// <remarks>
    /// Callers are expected to invoke this repeatedly from UI update paths until it returns true or
    /// <see cref="ResourceLoaded"/> notifies that the resource became available.
    /// </remarks>
    /// <param name="resourcePath">The rooted or relative resource path requested by the consumer.</param>
    /// <returns>
    /// <see langword="true"/> when the resource is already ready for use; otherwise <see langword="false"/>.
    /// </returns>
    public bool EnsureResource(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return false;

        var resPath = ToResPath(resourcePath);
        var resourceKey = resPath.ToString();

        if (IsResourceLoaded(resourceKey))
            return true;

        if (_failedResources.Contains(resourceKey))
            return false;

        if (!TryCheckResourceComplete(resourceKey, resPath, out var isComplete))
            return false;

        if (isComplete)
        {
            StartPreparingResource(resourceKey, resPath);
            return IsResourceLoaded(resourceKey);
        }

        _pendingResources[resourceKey] = resPath;

        if (!_requestedResources.Contains(resourceKey))
            RequestResource(resourceKey);

        return false;
    }

    /// <summary>
    /// Returns a ready-to-use texture that was loaded through the NetTextures pipeline.
    /// </summary>
    /// <param name="resourcePath">The resource path previously requested through <see cref="EnsureResource"/>.</param>
    /// <param name="texture">The ready texture when the method returns <see langword="true"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the texture is ready; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetTexture(string resourcePath, out Texture? texture)
    {
        texture = null;

        if (string.IsNullOrWhiteSpace(resourcePath))
            return false;

        var resourceKey = ToResPath(resourcePath).ToString();
        if (_loadedTextures.TryGetValue(resourceKey, out var loaded))
        {
            texture = loaded.Texture;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a ready-to-use RSI animation state that was loaded through the NetTextures pipeline.
    /// </summary>
    /// <param name="resourcePath">The RSI resource path previously requested through <see cref="EnsureResource"/>.</param>
    /// <param name="stateId">The RSI state identifier inside the uploaded resource.</param>
    /// <param name="state">The ready animation state when the method returns <see langword="true"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the requested state is ready; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetAnimationState(string resourcePath, string stateId, out NetTextureAnimationState? state)
    {
        state = null;

        if (string.IsNullOrWhiteSpace(resourcePath))
            return false;

        var resourceKey = ToResPath(resourcePath).ToString();
        if (!_loadedRsis.TryGetValue(resourceKey, out var loaded))
            return false;

        return loaded.States.TryGetValue(stateId, out state);
    }

    /// <summary>
    /// Resolves the in-memory uploaded path for a network resource.
    /// </summary>
    /// <param name="resourcePath">The rooted or relative resource path used by consumers.</param>
    /// <returns>The rooted path inside the mounted <c>/Uploaded</c> content root.</returns>
    public ResPath GetUploadedPath(string resourcePath)
    {
        var relativePath = ToResPath(resourcePath).ToRelativePath();
        return ((new ResPath(UploadedPrefix) / relativePath).ToRootedPath());
    }

    /// <summary>
    /// Sends a PDA photo capture to the server for dynamic NetTexture registration.
    /// </summary>
    /// <param name="loaderUid">The net entity that initiated the photo capture.</param>
    /// <param name="imageData">Encoded image bytes to register on the server.</param>
    /// <param name="width">The pixel width of the captured image.</param>
    /// <param name="height">The pixel height of the captured image.</param>
    public void SendPhotoToServer(NetEntity loaderUid, byte[] imageData, int width, int height)
    {
        if (!_netManager.IsConnected)
        {
            _sawmill.Warning("Cannot send photo: client not connected to server");
            return;
        }

        var message = new PdaPhotoCaptureMessage
        {
            LoaderUid = loaderUid,
            ImageData = imageData,
            Width = width,
            Height = height
        };

        _netManager.ClientSendMessage(message);
        _sawmill.Debug($"Sent photo to server: {width}x{height}, {imageData.Length} bytes, loader: {loaderUid}");
    }
    #endregion
}
